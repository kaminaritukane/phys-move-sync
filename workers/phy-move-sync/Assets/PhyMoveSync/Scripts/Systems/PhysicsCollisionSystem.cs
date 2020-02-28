using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace PhyMoveSync
{

    [DisableAutoCreation]
    [UpdateAfter(typeof(PhysicsMoveSystem))]
    public class PhysicsCollisionSystem : JobComponentSystem
    {
        private BuildPhysicsWorld m_BuildPhysicsWorldSystem;
        private StepPhysicsWorld m_StepPhysicsWorldSystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            JobHandle collisionEventHandle = new CollisionEventJob()
                .Schedule(m_StepPhysicsWorldSystem.Simulation,
                    ref m_BuildPhysicsWorldSystem.PhysicsWorld,
                    inputDeps);

            return collisionEventHandle;
        }

        struct CollisionEventJob : ICollisionEventsJob
        {
            public void Execute(CollisionEvent collisionEvent)
            {
                var entityA = collisionEvent.Entities.EntityA;
                var entityB = collisionEvent.Entities.EntityB;
                Debug.Log($"Collision Happend {entityA} <-> {entityB}");
            }
        }
    }
}