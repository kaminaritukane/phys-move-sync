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
                                if (EntityManager.HasComponent<Rotation>(entity))
                                {
                                    var rotComp = EntityManager.GetComponentData<Rotation>(entity);
                                    var speed = 1.0f;
                                    var forward = math.mul(rotComp.Value, Vector3.forward * speed);

                                    var acceleration = new MoveAcceleration { linear = forward };
                                    if ( EntityManager.HasComponent<MoveAcceleration>(entity) )
                                    {
                                        EntityManager.SetComponentData(entity, acceleration);
                                    }
                                    else
                                    {
                                        PostUpdateCommands.AddComponent<MoveAcceleration>(entity, acceleration);
                                    }
                                }
                                break;
                            case UnitAction.eUnitAction.StopMove:
                                {
                                    if (EntityManager.HasComponent<MoveAcceleration>(entity))
                                    {
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
    }
}

