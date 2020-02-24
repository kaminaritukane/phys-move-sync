using Improbable;
using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;

namespace PhyMoveSync
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSReceiveGroup.InternalSpatialOSReceiveGroup))]
    [UpdateBefore(typeof(EntitySystem))]
    public class ServerUnitCreatSystem : ComponentSystem
    {
        private EntitySystem entitySystem;
        private WorkerSystem workerSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            entitySystem = World.GetExistingSystem<EntitySystem>();
            workerSystem = World.GetExistingSystem<WorkerSystem>();
        }

        protected override void OnUpdate()
        {
            foreach (var entityId in entitySystem.GetEntitiesAdded())
            {
                workerSystem.TryGetEntity(entityId, out var entity);
                
                if ( EntityManager.HasComponent<Metadata.Component>(entity) )
                {
                    var md = EntityManager.GetComponentData<Metadata.Component>(entity);
                    Debug.Log($"[{World.Name}] Entity {entityId} {md.EntityType} created.");
                }
                else
                {
                    Debug.Log($"[{World.Name}] Entity {entityId} created.");
                }
            }

            var removedEntities = entitySystem.GetEntitiesRemoved();
            foreach (var entityId in removedEntities)
            {
                Debug.Log($"[{World.Name}] Entity {entityId} removed.");
            }
        }
    }
}
