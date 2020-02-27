using Improbable.Gdk.Core;
using Improbable.Gdk.StandardTypes;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace PhyMoveSync
{
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ClientMovementSyncSystem : ComponentSystem
    {
        private WorkerSystem workerSystem;
        private ComponentUpdateSystem componentUpdateSystem;

        private float cumulativeTimeDelta = 0f;
        private float updateDelta = 1.0f / 15.0f;
        private uint timestamp = 0;

        private readonly Dictionary<EntityId, Queue<ClientMoveRequest>> entitiesRequests =
            new Dictionary<EntityId, Queue<ClientMoveRequest>>();


        private EntityQuery unitsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            workerSystem = World.GetExistingSystem<WorkerSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

            unitsQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<PhysicsVelocity>(),
                ComponentType.ReadOnly<ClientMovement.Component>()
            );
        }

        protected override void OnUpdate()
        {
            var delta = cumulativeTimeDelta + UnityEngine.Time.deltaTime;
            if (delta >= updateDelta)
            {
                delta -= updateDelta;
                ++timestamp;

                Entities.With(unitsQuery).ForEach(
                    (ref SpatialEntityId spEntityId,
                     ref PhysicsVelocity phyVel,
                     ref ClientMovement.Component moveComp) =>
                    {
                        var latest = moveComp.Latest;
                        latest.LinearVelocity = phyVel.Linear.ToIntAbsolute();
                        latest.AngularVelocity = phyVel.Angular.ToIntAbsolute();
                        latest.Timestamp = timestamp;
                        latest.RequestTime = Time.time;
                        moveComp.Latest = latest;
                        //Debug.Log($"[Client] Sent frame:{latest.Timestamp}, tm:{latest.RequestTime}," +
                        //    $" Entity {spEntityId.EntityId}, " +
                        //    $" {latest.LinearVelocity.Value.ToFloat3()}" +
                        //    $" {latest.AngularVelocity.Value.ToFloat3()}");

                        // add to queue
                        if ( !entitiesRequests.TryGetValue(spEntityId.EntityId, out var requests) )
                        {
                            requests = new Queue<ClientMoveRequest>();
                            entitiesRequests.Add(spEntityId.EntityId, requests);
                        }
                        requests.Enqueue(latest); 
                    }
                );
            }

            cumulativeTimeDelta = delta;

            // receive server feedback for movement
            var serverUpdates = componentUpdateSystem.GetComponentUpdatesReceived<ServerMovement.Update>();
            for ( int i =0; i<serverUpdates.Count; ++i )
            {
                var serverUpdate = serverUpdates[i];
                if ( !serverUpdate.Update.Latest.HasValue )
                {
                    continue;
                }

                if (!workerSystem.TryGetEntity(serverUpdate.EntityId, out var entity))
                {
                    continue;
                }

                // reconcile
                if (entitiesRequests.TryGetValue(serverUpdate.EntityId, out var requests))
                {
                    var latestValue = serverUpdate.Update.Latest.Value;
                    Reconsile(entity, latestValue, requests);
                }
            }
        }

        private void Reconsile(Entity entity, ServerMoveResponse serverMoveResp,
            Queue<ClientMoveRequest> requests)
        {
            if (!EntityManager.HasComponent<Translation>(entity)
                || !EntityManager.HasComponent<Rotation>(entity)
                || !EntityManager.HasComponent<PhysicsVelocity>(entity))
            {
                return;
            }

            var transComp = EntityManager.GetComponentData<Translation>(entity);
            var rotComp = EntityManager.GetComponentData<Rotation>(entity);
            var phyVelComp = EntityManager.GetComponentData<PhysicsVelocity>(entity);

            var latestClientRequest = serverMoveResp.Request;

            var lastRequestTime = latestClientRequest.RequestTime;
            var f3LastVel = latestClientRequest.LinearVelocity.HasValue ?
                            latestClientRequest.LinearVelocity.Value.ToFloat3() : float3.zero;
            var f3LastAng = latestClientRequest.AngularVelocity.HasValue ?
                            latestClientRequest.AngularVelocity.Value.ToFloat3() : float3.zero;

            var authPos = serverMoveResp.Position;
            var authRot = serverMoveResp.Rotation;

            var predictionPos = authPos.HasValue ? authPos.Value.ToFloat3() : transComp.Value;
            var predictionRot = authRot.HasValue ? authRot.Value.ToUnityQuaternion() : rotComp.Value;

            foreach (var req in requests.ToArray())
            {
                if (req.Timestamp <= latestClientRequest.Timestamp)
                {
                    requests.Dequeue();
                }
                else
                {
                    var deltaTime = req.RequestTime - lastRequestTime;

                    // prediction position
                    {
                        var f3CurVel = req.LinearVelocity.HasValue ?
                            req.LinearVelocity.Value.ToFloat3() : float3.zero;

                        predictionPos += (f3LastVel + f3CurVel) * deltaTime * 0.5f;

                        f3LastVel = f3CurVel;
                    }

                    // prediction rotation
                    {

                        var f3CurAng = req.AngularVelocity.HasValue ?
                            req.AngularVelocity.Value.ToFloat3() : float3.zero;

                        var rotated = (f3LastAng + f3CurAng) * deltaTime * 0.5f;
                        predictionRot = math.mul(predictionRot, quaternion.EulerXYZ(rotated));

                        f3LastAng = f3CurAng;
                    }

                    lastRequestTime = req.RequestTime;
                }
            }

            var toNowTime = Time.time - lastRequestTime;

            bool bNeedUpdatePhyVel = false;
            // compare prediction pos and client simulation pos(current pos)
            {
                predictionPos += (f3LastVel + phyVelComp.Linear) * toNowTime * 0.5f;
                var deltaPos = transComp.Value - predictionPos;
                if (math.lengthsq(deltaPos) > 0.04f)
                {
                    //client simulation pos is to far from prediction Pos, need to update
                    Debug.Log($"Prediction delta distance:{math.length(deltaPos)}");

                    transComp.Value = predictionPos;
                    phyVelComp.Linear = f3LastVel;

                    EntityManager.SetComponentData(entity, transComp);
                    bNeedUpdatePhyVel = true;
                }
            }

            // compare prediction rotation and client simulation rotation(current rotation)
            {
                var toNowRotated = (f3LastAng + phyVelComp.Angular) * toNowTime * 0.5f;
                predictionRot = math.mul(predictionRot, quaternion.EulerXYZ(toNowRotated));

                var deltaRot = math.mul(predictionRot, math.inverse(rotComp.Value));

                var testV3 = math.mul(deltaRot, new float3(1, 1, 1));
                var radians = Vector3Extensions.Radians(testV3, new float3(1, 1, 1));
                if ( radians > 0.05f ) // about 3 degrees
                {
                    Debug.Log($"Prediction delta radians: {radians}");
                    rotComp.Value = predictionRot;
                    phyVelComp.Angular = f3LastAng;

                    EntityManager.SetComponentData(entity, rotComp);
                    bNeedUpdatePhyVel = true;
                }
            }

            if (bNeedUpdatePhyVel)
            {
                EntityManager.SetComponentData(entity, phyVelComp);
            }

            //if (latestClientRequest.LinearVelocity.HasValue
            //    && pos.HasValue)
            //{
            //    Debug.Log($"[Client] Recv frame:{latestClientRequest.Timestamp}, tm:{latestClientRequest.RequestTime}," +
            //        $" {latestClientRequest.LinearVelocity.Value.ToFloat3()}," +
            //        $" {pos.Value.ToFloat3()}");
            //}

        }
    }
}