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
                    ComponentType.ReadOnly<MoveAbility>()
                },
                Any = new ComponentType[] {
                    ComponentType.ReadOnly<MoveAcceleration>(),
                    ComponentType.ReadOnly<RotateAcceleration>(),
                    ComponentType.ReadOnly<StopMovement>(),
                    ComponentType.ReadOnly<StopRotation>(),
                }
            };
            physicsQuery = GetEntityQuery(queryDesc);

            CreatePhysicsStep();

            //CreateStaticUnit(@"Prefabs/Cube");
            //CreateDynimicUnit(@"Prefabs/Cube");
            //CreateDynimicUnit(@"Prefabs/UnityClient/Authority/Player");
        }

        void CreatePhysicsStep()
        {
            var entity = EntityManager.CreateEntity(new ComponentType[] { });

            EntityManager.AddComponentData(entity, new LocalToWorld { });
            EntityManager.AddComponentData(entity, new PhysicsStep
            {
                SimulationType = SimulationType.UnityPhysics,
                Gravity = float3.zero,
                SolverIterationCount = 4,
                ThreadCountHint = 8
            });
            EntityManager.AddComponentData(entity, new Rotation { Value = quaternion.identity });
            EntityManager.AddComponentData(entity, new Translation { Value = float3.zero });
        }

        private void CreateStaticUnit(string prefabPath)
        {
            var prefab = Resources.Load<GameObject>(prefabPath);
            var sourceEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
            var sourceCollider = EntityManager.GetComponentData<PhysicsCollider>(sourceEntity).Value;

            var entity = EntityManager.Instantiate(sourceEntity);
            EntityManager.SetComponentData(entity, new Translation { Value = new float3(0, 0, 0) });
            EntityManager.SetComponentData(entity, new Rotation { Value = Quaternion.identity });
            EntityManager.SetComponentData(entity, new PhysicsCollider { Value = sourceCollider });
        }

        private void CreateDynimicUnit(string prefabPath)
        {
            var prefab = Resources.Load<GameObject>(prefabPath);
            var sourceEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, World.Active);
            var sourceCollider = EntityManager.GetComponentData<PhysicsCollider>(sourceEntity).Value;

            var entity = EntityManager.Instantiate(sourceEntity);
            EntityManager.SetComponentData(entity, new Translation { Value = new float3(0, 0, 5) });
            EntityManager.SetComponentData(entity, new Rotation { Value = Quaternion.identity });

            var colliderComp = new PhysicsCollider
            {
                Value = sourceCollider
            };
            EntityManager.SetComponentData(entity, colliderComp);
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
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer();
            JobHandle moveJobHandle = new MovePhysicsUnitJob
            {
                ecb = ecb,
                entities = physicsQuery.ToEntityArray(Allocator.TempJob),
                velocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
                moveAbilityGroup = GetComponentDataFromEntity<MoveAbility>(true),
                rotationGroup = GetComponentDataFromEntity<Rotation>(true),
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

        struct MovePhysicsUnitJob : IJob
        {
            public EntityCommandBuffer ecb;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> entities;

            public ComponentDataFromEntity<PhysicsVelocity> velocityGroup;

            [ReadOnly] public ComponentDataFromEntity<MoveAbility> moveAbilityGroup;
            [ReadOnly] public ComponentDataFromEntity<Rotation> rotationGroup;
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
                    var moveAbility = moveAbilityGroup[entity];
                    var rot = rotationGroup[entity];

                    // linear
                    bool isStopMove = stopMovementGroup.HasComponent(entity);
                    if ( isStopMove )
                    {
                        var moveLinear = moveAbility.linearAcceleration * deltaTime;
                        var sqMoveLinear = moveLinear * moveLinear;
                        var sqPhyVel = math.lengthsq(phyVel.Linear);
                        if (sqPhyVel < sqMoveLinear)// stopped
                        {
                            phyVel.Linear = float3.zero;
                            ecb.RemoveComponent<MoveAcceleration>(entity);
                            ecb.RemoveComponent<StopMovement>(entity);
                        }
                        else
                        {
                            var stopDir = math.normalize(-phyVel.Linear);
                            var stopAcc = stopDir * moveAbility.linearAcceleration;
                            phyVel.Linear += stopAcc * deltaTime;
                        }
                    }
                    else
                    {
                        if (moveAccelerationGroup.HasComponent(entity))
                        {
                            var moveAcc = moveAccelerationGroup[entity];
                            var moveLinear = math.mul(rot.Value, moveAcc.localLinear * deltaTime);

                            phyVel.Linear += moveLinear;

                            var maxLinearSq = moveAbility.maxLinearSpeed * moveAbility.maxLinearSpeed;
                            if (math.lengthsq(phyVel.Linear) > maxLinearSq)
                            {
                                phyVel.Linear = math.normalizesafe(phyVel.Linear) * moveAbility.maxLinearSpeed;
                            }
                        }
                    }
                    //Debug.Log($"phyVel Linear: {phyVel.Linear}");

                    // angular
                    bool isStopRotate = stopRotationGroup.HasComponent(entity);
                    if (isStopRotate)
                    {
                        var rotateAngular = moveAbility.angularAcceleration * deltaTime;
                        var sqRotateAngular = rotateAngular * rotateAngular;
                        var sqPhyAng = math.lengthsq(phyVel.Angular);
                        if (sqPhyAng < sqRotateAngular) // stopped 
                        {
                            phyVel.Angular = float3.zero;
                            ecb.RemoveComponent<RotateAcceleration>(entity);
                            ecb.RemoveComponent<StopRotation>(entity);
                        }
                        else
                        {
                            var stopRot = math.normalize(-phyVel.Angular);
                            var stopAcc = stopRot * moveAbility.angularAcceleration;
                            phyVel.Angular += stopAcc * deltaTime;
                        }
                    }
                    else
                    {
                        if (rotateAccelerationGroup.HasComponent(entity))
                        {
                            var roteAcc = rotateAccelerationGroup[entity];
                            var roteAngular = roteAcc.angular * deltaTime;
                            phyVel.Angular += roteAngular;

                            var maxAngularSpeed3 = new float3(moveAbility.maxAngularSpeed);
                            phyVel.Angular = math.clamp(phyVel.Angular, -maxAngularSpeed3, maxAngularSpeed3);
                        }
                    }
                    //Debug.Log($"phyVel Angular: {phyVel.Angular}");

                    velocityGroup[entity] = phyVel;
                }
            }
        }
    }
}