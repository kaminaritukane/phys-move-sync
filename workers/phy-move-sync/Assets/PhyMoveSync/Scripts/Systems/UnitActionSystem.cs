using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PhyMoveSync
{
    [DisableAutoCreation]
    [UpdateAfter(typeof(InputSystem))]
    public class UnitActionSystem : ComponentSystem
    {
        private EntityQuery actionQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            actionQuery = GetEntityQuery(
                ComponentType.ReadOnly<UnitAction>()
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(actionQuery).ForEach(
                ( Entity entity ) => 
                {
                    var actions = EntityManager.GetBuffer<UnitAction>(entity);

                    // TODO: do actions
                    foreach( var act in actions )
                    {
                        switch(act.action)
                        {
                            case UnitAction.eUnitAction.MoveForward:
                                AddMoveAcceleration(entity, Vector3.forward, act.parameter);
                                break;
                            case UnitAction.eUnitAction.StopMoveForward:
                                AddMoveAcceleration(entity, Vector3.forward, -act.parameter);
                                break;
                            case UnitAction.eUnitAction.MoveRight:
                                AddMoveAcceleration(entity, Vector3.right, act.parameter);
                                break;
                            case UnitAction.eUnitAction.StopMoveRight:
                                AddMoveAcceleration(entity, Vector3.right, -act.parameter);
                                break;
                            case UnitAction.eUnitAction.AutoStopMove:
                                {
                                    if (EntityManager.HasComponent<MoveAcceleration>(entity))
                                    {
                                        var acceleration = new MoveAcceleration { linear = float3.zero };
                                        PostUpdateCommands.AddComponent<StopMovement>(entity);
                                    }
                                    // no UnitAcceleratio, means stopped
                                }
                                break;
                        }
                    }

                    actions.Clear();
                }
            );
        }

        private void AddMoveAcceleration(Entity entity, Vector3 dir, float parameter)
        {
            if (EntityManager.HasComponent<Rotation>(entity)
                && EntityManager.HasComponent<MoveAbility>(entity))
            {
                var rotComp = EntityManager.GetComponentData<Rotation>(entity);
                var moveAbility = EntityManager.GetComponentData<MoveAbility>(entity);
                var linearAcc = math.mul(rotComp.Value, dir * parameter * moveAbility.linearAcceleration);

                if (EntityManager.HasComponent<MoveAcceleration>(entity))
                {
                    var accComp = EntityManager.GetComponentData<MoveAcceleration>(entity);
                    accComp.linear += linearAcc;
                    EntityManager.SetComponentData(entity, accComp);
                }
                else
                {
                    var acceleration = new MoveAcceleration { linear = linearAcc };
                    PostUpdateCommands.AddComponent<MoveAcceleration>(entity, acceleration);
                }

                if (EntityManager.HasComponent<StopMovement>(entity))
                {
                    PostUpdateCommands.RemoveComponent<StopMovement>(entity);
                }
            }
        }
    }
}

