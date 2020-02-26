using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Improbable.Gdk.Core;
using Improbable.Gdk.StandardTypes;

namespace PhyMoveSync
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ServerMovementSyncSystem : ComponentSystem
    {
        private WorkerSystem workerSystem;
        private ComponentUpdateSystem componentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            workerSystem = World.GetExistingSystem<WorkerSystem>();
            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            var clientUpdates = componentUpdateSystem.GetComponentUpdatesReceived<ClientMovement.Update>();
            for( int i=0; i < clientUpdates.Count; ++i )
            {
                var clientUpdate = clientUpdates[i];
                if ( !clientUpdate.Update.Latest.HasValue )
                {
                    continue;
                }

                if (!workerSystem.TryGetEntity(clientUpdate.EntityId, out var entity)
                    || !EntityManager.HasComponent<UnitMoveAbility.Component>(entity)
                    || !EntityManager.HasComponent<ServerMovement.Component>(entity)
                    || !EntityManager.HasComponent<Translation>(entity)
                    || !EntityManager.HasComponent<Rotation>(entity) )
                {
                    continue;
                }

                var serveMoveComp = EntityManager.GetComponentData<ServerMovement.Component>(entity);
                var serverMoveLatest = serveMoveComp.Latest;

                // update serverMoveLatest
                {
                    var clientLatestValue = clientUpdate.Update.Latest.Value;
                    var moveAbility = EntityManager.GetComponentData<UnitMoveAbility.Component>(entity);

                    // position
                    var transComp = EntityManager.GetComponentData<Translation>(entity);
                    serverMoveLatest.Position = transComp.Value.ToIntAbsolute();

                    // rotation
                    var rotComp = EntityManager.GetComponentData<Rotation>(entity);
                    serverMoveLatest.Rotation = rotComp.Value.ToCompressedQuaternion();

                    var clientRequest = serverMoveLatest.Request;
                    // check the request from client
                    {
                        // linear
                        if (clientLatestValue.LinearVelocity.HasValue)
                        {
                            var linearVel = clientLatestValue.LinearVelocity.Value.ToFloat3();
                            var maxLinearSpeed = moveAbility.MaxLinearSpeed.ToFloat10k();
                            if (math.lengthsq(linearVel) > maxLinearSpeed * maxLinearSpeed)
                            {
                                // client cheating?
                                Debug.LogWarning($"[Server] Client cheating on linear velocity!");
                                linearVel = math.normalizesafe(linearVel) * maxLinearSpeed;
                            }

                            clientRequest.LinearVelocity = linearVel.ToIntAbsolute();
                        }

                        // angular
                        if (clientLatestValue.AngularVelocity.HasValue)
                        {
                            var angularVel = clientLatestValue.AngularVelocity.Value.ToFloat3();
                            var maxAngularSpeed = moveAbility.MaxAngularSpeed.ToFloat10k();
                            if (math.abs(angularVel.x) > maxAngularSpeed
                                || math.abs(angularVel.y) > maxAngularSpeed
                                || math.abs(angularVel.z) > maxAngularSpeed)
                            {
                                Debug.LogWarning($"[Server] Client cheating on angular velocity!");
                                var maxAngularSpeed3 = new float3(maxAngularSpeed);
                                angularVel = math.clamp(angularVel, -maxAngularSpeed3, maxAngularSpeed3);
                            }

                            clientRequest.AngularVelocity = angularVel.ToIntAbsolute();
                        }

                        // timestamp
                        clientRequest.Timestamp = clientLatestValue.Timestamp;

                        // request time
                        clientRequest.RequestTime = clientLatestValue.RequestTime;
                    }
                    serverMoveLatest.Request = clientRequest;
                }
                serveMoveComp.Latest = serverMoveLatest;

                EntityManager.SetComponentData(entity, serveMoveComp);
            }
        }
    }
}