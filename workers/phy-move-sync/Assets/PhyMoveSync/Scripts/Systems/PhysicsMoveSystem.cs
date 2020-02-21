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
    public class PhysicsMoveSystem : JobComponentSystem
    {
        private EntityQuery physicsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            physicsQuery = GetEntityQuery(
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Rotation>(),
                ComponentType.ReadOnly<PhysicsCollider>()
            );

            CreateUnit(@"Prefabs/Units/BlueShip");
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            JobHandle moveJobHandle = new MovePhysicsCubeJob
            {
                moveEntities = physicsQuery.ToEntityArray(Allocator.TempJob),
                velocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
                //rotateDirGroup = GetComponentDataFromEntity<RotateDir>(true),
                deltaTime = Time.deltaTime
            }.Schedule(inputDeps);

            return moveJobHandle;
        }

        private void CreateUnit(string prefabPath)
        {
            var prefab = Resources.Load<GameObject>(prefabPath);
            var sourceEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
            var sourceCollider = EntityManager.GetComponentData<PhysicsCollider>(sourceEntity).Value;

            var instance = EntityManager.Instantiate(sourceEntity);
            EntityManager.SetComponentData(instance, new Translation { Value = new float3(0, 0, 5) });
            EntityManager.SetComponentData(instance, new Rotation { Value = Quaternion.identity });
            EntityManager.SetComponentData(instance, new PhysicsCollider { Value = sourceCollider });
        }

        struct MovePhysicsCubeJob : IJob
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> moveEntities;

            public ComponentDataFromEntity<PhysicsVelocity> velocityGroup;
            //[ReadOnly] public ComponentDataFromEntity<RotateDir> rotateDirGroup;
            [ReadOnly] public float deltaTime;

            public void Execute()
            {
                for (int i = 0; i < moveEntities.Length; ++i)
                {
                    var entity = moveEntities[i];
                    var phyVel = velocityGroup[entity];
                    //var rotDir = rotateDirGroup[entity];
                    //MoveForward(ref phyVel, ref rotDir);
                    velocityGroup[entity] = phyVel;
                }
            }

            //private void MoveForward(ref PhysicsVelocity phyVel, ref RotateDir rotDir)
            //{
            //    var curSpeed = math.length(phyVel.Linear);
            //    if (curSpeed > 0.0f)
            //    {
            //        var curDir = math.normalize(phyVel.Linear);
            //        phyVel.Linear += curDir * (rotDir.speed - curSpeed) * deltaTime;
            //    }
            //    else
            //    {
            //        phyVel.Linear = random.NextFloat3();
            //    }
            //}
        }
    }
}