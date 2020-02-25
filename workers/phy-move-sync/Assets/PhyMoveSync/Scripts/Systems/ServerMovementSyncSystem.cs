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
        private ComponentUpdateSystem componentUpdateSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            componentUpdateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            var clientUpdates = componentUpdateSystem.GetComponentUpdatesReceived<ClientMovement.Update>();
            for( int i=0; i < clientUpdates.Count; ++i )
            {
                var clientUpdate = clientUpdates[i];
                Debug.Log($"[{World.Name}] clientUpdate: {clientUpdate.Update.Latest}");
            }
        }
    }
}