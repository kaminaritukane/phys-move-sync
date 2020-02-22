﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace PhyMoveSync
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(UnitActionSystem))]
    public class PhysicsMoveSystem : JobComponentSystem
    {
        private EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;

        private EntityQuery physicsQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] {
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<Rotation>(),
                    ComponentType.ReadOnly<PhysicsCollider>(),
                    ComponentType.ReadWrite<PhysicsVelocity>(),
                },
                Any = new ComponentType[] {
                    ComponentType.ReadOnly<MoveAcceleration>(),
                    ComponentType.ReadOnly<RotateAcceleration>(),
                    ComponentType.ReadOnly<StopMovement>(),
                    ComponentType.ReadOnly<StopRotation>(),
                }
            };
            physicsQuery = GetEntityQuery(queryDesc);

            CreateUnit(@"Prefabs/Units/BlueShip");
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer();
            JobHandle moveJobHandle = new MovePhysicsUnitJob
            {
                ecb = ecb,
                entities = physicsQuery.ToEntityArray(Allocator.TempJob),
                velocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
                moveAccelerationGroup = GetComponentDataFromEntity<MoveAcceleration>(true),
                rotateAccelerationGroup = GetComponentDataFromEntity<RotateAcceleration>(true),
                stopMovementGroup = GetComponentDataFromEntity<StopMovement>(true),
                stopRotationGroup = GetComponentDataFromEntity<StopRotation>(true),
                deltaTime = Time.deltaTime
            }.Schedule(inputDeps);

            // Make sure that the ECB system knows about our job
            m_EndSimulationEcbSystem.AddJobHandleForProducer(moveJobHandle);

            return moveJobHandle;
        }

        private void CreateUnit(string prefabPath)
        {
            var prefab = Resources.Load<GameObject>(prefabPath);
            var sourceEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
            var sourceCollider = EntityManager.GetComponentData<PhysicsCollider>(sourceEntity).Value;

            var entity = EntityManager.Instantiate(sourceEntity);
            EntityManager.SetComponentData(entity, new Translation { Value = new float3(0, 0, 5) });
            EntityManager.SetComponentData(entity, new Rotation { Value = Quaternion.identity });
            EntityManager.SetComponentData(entity, new PhysicsCollider { Value = sourceCollider });
            EntityManager.SetComponentData(entity, new PhysicsVelocity { Linear = new float3(0, 0, 0) });

            EntityManager.AddComponentData(entity, new InputReceiver { 
                hasMoveInput = false, 
                hasRotateInput = false 
            });

            EntityManager.AddBuffer<UnitAction>(entity);
        }

        struct MovePhysicsUnitJob : IJob
        {
            public EntityCommandBuffer ecb;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> entities;

            public ComponentDataFromEntity<PhysicsVelocity> velocityGroup;
            [ReadOnly] public ComponentDataFromEntity<MoveAcceleration> moveAccelerationGroup;
            [ReadOnly] public ComponentDataFromEntity<RotateAcceleration> rotateAccelerationGroup;
            [ReadOnly] public ComponentDataFromEntity<StopMovement> stopMovementGroup;
            [ReadOnly] public ComponentDataFromEntity<StopRotation> stopRotationGroup;
            [ReadOnly] public float deltaTime;

            public void Execute()
            {
                for (int i = 0; i < entities.Length; ++i)
                {
                    var entity = entities[i];
                    var phyVel = velocityGroup[entity];

                    // linear
                    if (moveAccelerationGroup.HasComponent(entity))
                    {
                        var moveAcc = moveAccelerationGroup[entity];
                        bool isStopMove = stopMovementGroup.HasComponent(entity);
                        var moveLinear = moveAcc.linear * deltaTime;
                        phyVel.Linear += moveLinear * (isStopMove ? -1f : 1f);

                        var sqMoveLinear = Unity.Mathematics.math.lengthsq(moveLinear);
                        var sqPhyVel = Unity.Mathematics.math.lengthsq(phyVel.Linear);
                        if (isStopMove
                            && sqPhyVel < sqMoveLinear)
                        {
                            phyVel.Linear = float3.zero;
                            ecb.RemoveComponent<MoveAcceleration>(entity);
                            ecb.RemoveComponent<StopMovement>(entity);
                        }
                    }

                    // angular
                    if (rotateAccelerationGroup.HasComponent(entity))
                    {
                        var angAcc = rotateAccelerationGroup[entity];
                        bool isStopRotate = stopRotationGroup.HasComponent(entity);
                        phyVel.Angular += angAcc.angular * deltaTime * (isStopRotate ? -1f : 1f);

                        if (isStopRotate
                            && Unity.Mathematics.math.lengthsq(phyVel.Angular) < 1.0e-6f)
                        {
                            ecb.RemoveComponent<RotateAcceleration>(entity);
                            ecb.RemoveComponent<StopRotation>(entity);
                        }
                    }

                    velocityGroup[entity] = phyVel;
                }
            }
        }
    }
}