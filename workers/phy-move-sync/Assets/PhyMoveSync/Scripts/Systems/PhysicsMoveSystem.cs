using Improbable.Gdk.StandardTypes;
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

        private float cumulativeTimeDelta = 0f;// Need to set a global cumulativeTimeDelta
        private float updateDelta = 1.0f / 15.0f;

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
                    ComponentType.ReadOnly<UnitMoveAbility.Component>()
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

            EntityManager.AddComponentData(entity, new UnitMoveAbility.Component
            {
                LinearAcceleration = 1f.ToInt10k(),
                AngularAcceleration = 1f.ToInt10k(),
                MaxLinearSpeed = 3f.ToInt10k(),
                MaxAngularSpeed = 2f.ToInt10k()
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
            JobHandle jobRet = inputDeps;

            var delta = cumulativeTimeDelta + UnityEngine.Time.deltaTime;
            if (delta >= updateDelta)
            {
                delta -= updateDelta;

                var ecb = m_EndSimulationEcbSystem.CreateCommandBuffer();
                JobHandle moveJobHandle = new MovePhysicsUnitJob
                {
                    ecb = ecb,
                    entities = physicsQuery.ToEntityArray(Allocator.TempJob),
                    velocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
                    moveAbilityGroup = GetComponentDataFromEntity<UnitMoveAbility.Component>(true),
                    rotationGroup = GetComponentDataFromEntity<Rotation>(true),
                    moveAccelerationGroup = GetComponentDataFromEntity<MoveAcceleration>(true),
                    rotateAccelerationGroup = GetComponentDataFromEntity<RotateAcceleration>(true),
                    stopMovementGroup = GetComponentDataFromEntity<StopMovement>(true),
                    stopRotationGroup = GetComponentDataFromEntity<StopRotation>(true),
                    deltaTime = updateDelta
                }.Schedule(inputDeps);

                // Make sure that the ECB system knows about our job
                m_EndSimulationEcbSystem.AddJobHandleForProducer(moveJobHandle);

                jobRet = moveJobHandle;
            }
            cumulativeTimeDelta = delta;

            return jobRet;
        }

        struct MovePhysicsUnitJob : IJob
        {
            public EntityCommandBuffer ecb;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> entities;

            public ComponentDataFromEntity<PhysicsVelocity> velocityGroup;

            [ReadOnly] public ComponentDataFromEntity<UnitMoveAbility.Component> moveAbilityGroup;
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
                    if (isStopMove)
                    {
                        var linearAcc = moveAbility.LinearAcceleration.ToFloat10k();
                        var moveLinear = linearAcc * deltaTime;
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
                            var stopAcc = stopDir * linearAcc;
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

                            var maxLinearSpeed = moveAbility.MaxLinearSpeed.ToFloat10k();
                            var maxLinearSq = maxLinearSpeed * maxLinearSpeed;
                            if (math.lengthsq(phyVel.Linear) > maxLinearSq)
                            {
                                phyVel.Linear = math.normalizesafe(phyVel.Linear) * maxLinearSpeed;
                            }
                        }
                    }
                    //Debug.Log($"phyVel Linear: {phyVel.Linear}");

                    // angular
                    bool isStopRotate = stopRotationGroup.HasComponent(entity);
                    if (isStopRotate)
                    {
                        var angularAcc = moveAbility.AngularAcceleration.ToFloat10k();
                        var rotateAngular = angularAcc * deltaTime;
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
                            var stopAcc = stopRot * angularAcc;
                            phyVel.Angular += stopAcc * deltaTime;
                        }
                    }
                    else
                    {
                        if (rotateAccelerationGroup.HasComponent(entity))
                        {
                            var roteAcc = rotateAccelerationGroup[entity];
                            var roteAngular = roteAcc.angularAcc * deltaTime;
                            phyVel.Angular += roteAngular;

                            var maxAngularSpeed = moveAbility.MaxAngularSpeed.ToFloat10k();
                            var maxAngularSpeed3 = new float3(maxAngularSpeed);
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