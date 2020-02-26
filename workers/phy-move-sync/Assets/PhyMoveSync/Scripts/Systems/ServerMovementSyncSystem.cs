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

                if (!workerSystem.TryGetEntity(clientUpdate.EntityId, out var entity))
                {
                    continue;
                }

                if ( !EntityManager.HasComponent<UnitMoveAbility.Component>(entity) )
                {
                    continue;
                }

                if (!EntityManager.HasComponent<ServerMovement.Component>(entity))
                {
                    continue;
                }

                var serveMoveComp = EntityManager.GetComponentData<ServerMovement.Component>(entity);
                var serverMoveLatest = serveMoveComp.Latest;

                var latestValue = clientUpdate.Update.Latest.Value;
                var moveAbility = EntityManager.GetComponentData<UnitMoveAbility.Component>(entity);
                if (latestValue.LinearVelocity.HasValue )
                {
                    var linearVel = latestValue.LinearVelocity.Value.ToFloat3();
                    var maxLinearSpeed = moveAbility.MaxLinearSpeed.ToFloat10k();
                    if ( math.lengthsq(linearVel) > maxLinearSpeed * maxLinearSpeed )
                    {
                        // client cheating?
                        linearVel = math.normalizesafe(linearVel) * maxLinearSpeed;
                        Debug.LogWarning($"Client cheating!");
                    }

                    serverMoveLatest.LinearVelocity = linearVel.ToIntAbsolute();
                }

                serveMoveComp.Latest = serverMoveLatest;

                EntityManager.SetComponentData(entity, serveMoveComp);
            }
        }
    }
}