using Improbable.Gdk.Core;
using Improbable.Gdk.StandardTypes;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
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

        private readonly Queue<ClientMoveRequest> requests = new Queue<ClientMoveRequest>();

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
                        Debug.Log($"[Client] Sent frame:{latest.Timestamp}, tm:{latest.RequestTime}," +
                            $" Entity {spEntityId.EntityId}, " +
                            $" {latest.LinearVelocity.Value.ToFloat3()}" +
                            $" {latest.AngularVelocity.Value.ToFloat3()}");

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

                var latestValue = serverUpdate.Update.Latest.Value;
                var latestClientRequest = latestValue.Request;

                // position reconcile
                if (latestClientRequest.LinearVelocity.HasValue
                    && latestValue.Position.HasValue)
                {
                    Debug.Log($"[Client] Recv frame:{latestClientRequest.Timestamp}, tm:{latestClientRequest.RequestTime}," +
                        $" Entity {serverUpdate.EntityId}" +
                        $" {latestClientRequest.LinearVelocity.Value.ToFloat3()}," +
                        $" {latestValue.Position.Value.ToFloat3()}");


                }
            }
        }
    }
}