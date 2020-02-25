using Improbable.Gdk.Core;
using Improbable.Gdk.StandardTypes;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using UnityEngine;

namespace PhyMoveSync
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ClientMovementSyncSystem : ComponentSystem
    {
        private float cumulativeTimeDelta = 0f;
        private float updateDelta = 1.0f / 15.0f;
        private uint timestamp = 0;

        //private ComponentUpdateSystem componentUpdateSystem;

        private EntityQuery unitsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            //componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

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
                        Debug.Log($"[Client] Entity {spEntityId.EntityId}, update {latest}");
                    }
                );
            }

            cumulativeTimeDelta = delta;

            // TODO: receive job
        }
    }
}