using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace PhyMoveSync
{
    using PrefabInfos = Tuple<RenderMesh,
                BlobAssetReference<Unity.Physics.Collider>,
                float3>;
    using PrefabStringToInfos = Dictionary<string,
                Tuple<RenderMesh,
                BlobAssetReference<Unity.Physics.Collider>,
                float3>>;

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSReceiveGroup.InternalSpatialOSReceiveGroup))]
    [UpdateBefore(typeof(EntitySystem))]
    public class ClientUnitCreatSystem : ComponentSystem
    {
        private EntitySystem entitySystem;
        private WorkerSystem workerSystem;

        private readonly Vector3 workerOrigin;

        private readonly PrefabStringToInfos prefabs;

        public ClientUnitCreatSystem(Vector3 origin)
        {
            workerOrigin = origin;

            prefabs = new PrefabStringToInfos();
        }

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

                if ( !EntityManager.HasComponent<Metadata.Component>(entity) )
                {
                    continue;
                }

                var metaData = EntityManager.GetComponentData<Metadata.Component>(entity);
                if (!EntityManager.HasComponent<Improbable.Position.Component>(entity))
                {
                    throw new InvalidOperationException($"Entity {metaData.EntityType}" +
                        $" does not have the Position component");
                }

                var posComp = EntityManager.GetComponentData<Improbable.Position.Component>(entity);
                var position = posComp.Coords.ToUnityVector() + workerOrigin;
                var rotation = quaternion.identity;

                var spatialOSEntityId = EntityManager.GetComponentData<SpatialEntityId>(entity);
                var authority = PlayerLifecycleHelper.IsOwningWorker(spatialOSEntityId.EntityId, World);
                Debug.Log($"[{World.Name}] Entity {entityId} {metaData.EntityType}" +
                    $" authority:{authority} created.");

                var authString = authority ? "Authority" : "NonAuthority";
                var prefabPath = Path.Combine("Prefabs", $"{UnityClientConnector.WorkerType}",
                    $"{authString}", $"{metaData.EntityType}");

                UpdateClientEntity(entity, prefabPath, position, rotation);
            }

            var removedEntities = entitySystem.GetEntitiesRemoved();
            foreach (var entityId in removedEntities)
            {
                Debug.Log($"[{World.Name}] Entity {entityId} removed.");
            }
        }

        private void UpdateClientEntity(Entity entity, string prefabPath,
            float3 position, quaternion orientation)
        {
            var prefabInfo = GetPrefabInfo(prefabPath);
            if (prefabInfo == null)
            {
                return;
            }

            if ( EntityManager.HasComponent<RenderMesh>(entity) )
            {
                EntityManager.SetSharedComponentData(entity, prefabInfo.Item1);
            }
            else
            {
                EntityManager.AddSharedComponentData(entity, prefabInfo.Item1);
            }

            EntityManager.AddComponentData(entity, new LocalToWorld { });
            EntityManager.AddComponentData(entity, new Translation { Value = position });
            EntityManager.AddComponentData(entity, new Rotation { Value = orientation });
            EntityManager.AddComponentData(entity, new NonUniformScale { Value = prefabInfo.Item3 });

            var colliderComp = new PhysicsCollider
            {
                Value = prefabInfo.Item2
            };
            EntityManager.AddComponentData(entity, colliderComp);
            EntityManager.AddComponentData(entity, new PhysicsVelocity { Linear = new float3(0, 0, 0) });
            EntityManager.AddComponentData(entity, PhysicsMass.CreateDynamic(
                colliderComp.MassProperties, 1f
            ));
            EntityManager.AddComponentData(entity, new PhysicsDamping
            {
                Linear = 0.01f,
                Angular = 0.03f
            });

            EntityManager.AddComponentData(entity, new MoveAbility
            {
                linearAcceleration = 1f,
                angularAcceleration = 1f,
                maxLinearSpeed = 3f,
                maxAngularSpeed = 2f
            });

            EntityManager.AddComponentData(entity, new InputReceiver
            {
                hasMoveInput = false,
                hasRotateInput = false
            });

            EntityManager.AddBuffer<UnitAction>(entity);

            // test
            {
                var prefab2 = Resources.Load<GameObject>(@"Prefabs/Cube");
                var meshData = prefab2.GetComponent<MeshFilter>().sharedMesh;
                var material = prefab2.GetComponent<MeshRenderer>().sharedMaterial;

                var meshRender = new RenderMesh()
                {
                    mesh = meshData,
                    material = material
                };

                Entity testEntity = EntityManager.CreateEntity(new ComponentType[] { });

                EntityManager.AddSharedComponentData(testEntity, meshRender);

                EntityManager.AddComponentData(testEntity, new LocalToWorld { });
                EntityManager.AddComponentData(testEntity, new Translation { Value = workerOrigin + new Vector3(0, 0, 5) });
                EntityManager.AddComponentData(testEntity, new Rotation { Value = Quaternion.AngleAxis(-90f, Vector3.right) });

                var sharedCollider2 = Unity.Physics.BoxCollider.Create(float3.zero,
                    quaternion.identity, new float3(1, 1, 1), 0.05f,
                    null,
                    new Unity.Physics.Material
                    {
                        Friction = 0f,
                        Restitution = 1f
                    }
                );
                EntityManager.AddComponentData(testEntity, new PhysicsCollider { Value = sharedCollider2 });
            }
        }

        private PrefabInfos GetPrefabInfo(string prefabPath)
        {
            if (!prefabs.TryGetValue(prefabPath, out var prefabInfo))
            {
                var prefab = Resources.Load<GameObject>(prefabPath);
                if (prefab == null)
                {
                    return null; // no prefab for this entity
                }

                var meshData = prefab.GetComponent<MeshFilter>().sharedMesh;
                var material = prefab.GetComponent<MeshRenderer>().sharedMaterial;

                var scale3 = prefab.GetComponent<Transform>().localScale;

                var renderMesh = new RenderMesh()
                {
                    mesh = meshData,
                    material = material
                };

                var render = prefab.GetComponent<Renderer>();
                var sharedCollider = Unity.Physics.BoxCollider.Create(float3.zero,
                    quaternion.identity,
                    scale3,
                    0.05f,
                    null,
                    new Unity.Physics.Material
                    {
                        Friction = 0f,
                        Restitution = 1f,
                        Flags = Unity.Physics.Material.MaterialFlags.EnableCollisionEvents
                    }
                );

                prefabInfo = new Tuple<RenderMesh,
                    BlobAssetReference<Unity.Physics.Collider>,
                    float3>(
                    renderMesh,
                    sharedCollider,
                    scale3
                );

                prefabs.Add(prefabPath, prefabInfo);
            }

            return prefabInfo;
        }
    }
}
