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

        private readonly float3 workerOrigin;

        public ServerMovementSyncSystem(Vector3 origin)
        {
            workerOrigin = origin;
        }

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
                if (!clientUpdate.Update.Latest.HasValue)
                {
                    continue;
                }

                if (!workerSystem.TryGetEntity(clientUpdate.EntityId, out var entity)
                    || !EntityManager.HasComponent<UnitMoveAbility.Component>(entity)
                    || !EntityManager.HasComponent<ServerMovement.Component>(entity)
                    || !EntityManager.HasComponent<Translation>(entity)
                    || !EntityManager.HasComponent<Rotation>(entity))
                {
                    continue;
                }

                var clientLatestValue = clientUpdate.Update.Latest.Value;

                UpdateServerMoveComponent(clientLatestValue, entity);

                // Apply move quesnt to server physics
                if (EntityManager.HasComponent<PhysicsVelocity>(entity))
                {
                    var phyVel = EntityManager.GetComponentData<PhysicsVelocity>(entity);

                    var serveMoveComp = EntityManager.GetComponentData<ServerMovement.Component>(entity);
                    var serverMoveLatest = serveMoveComp.Latest;
                    var clientRequest = serverMoveLatest.Request;// here the request has been verified 

                    if (clientRequest.LinearVelocity.HasValue)
                    {
                        phyVel.Linear = clientRequest.LinearVelocity.Value.ToFloat3();
                    }
                    if (clientRequest.AngularVelocity.HasValue)
                    {
                        phyVel.Angular = clientRequest.AngularVelocity.Value.ToFloat3();
                    }

                    EntityManager.SetComponentData(entity, phyVel);
                }
            }
        }

        private void UpdateServerMoveComponent(ClientMoveRequest clientLatestValue, Entity entity)
        {
            var serveMoveComp = EntityManager.GetComponentData<ServerMovement.Component>(entity);
            var serverMoveLatest = serveMoveComp.Latest;

            // update serverMoveLatest
            {
                var moveAbility = EntityManager.GetComponentData<UnitMoveAbility.Component>(entity);

                // position
                var transComp = EntityManager.GetComponentData<Translation>(entity);
                serverMoveLatest.Position = (transComp.Value - workerOrigin).ToIntAbsolute();

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
                            Debug.LogWarning($"[Server] Client cheating on linear velocity!" +
                                $" vel: {math.length(linearVel)}, max: {maxLinearSpeed}");
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
                            Debug.LogWarning($"[Server] Client cheating on angular velocity!" +
                                $" ang: {angularVel}, max: {maxAngularSpeed}");
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