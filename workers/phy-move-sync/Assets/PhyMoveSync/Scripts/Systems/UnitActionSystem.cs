using System;
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

                    foreach( var act in actions )
                    {
                        switch(act.action)
                        {
                            case UnitAction.eUnitAction.MoveForward:
                                AddMoveAcceleration(entity, MoveAcceleration.Direction.Forward, act.parameter);
                                break;
                            case UnitAction.eUnitAction.StopMoveForward:
                                AddMoveAcceleration(entity, MoveAcceleration.Direction.Forward, -act.parameter);
                                break;
                            case UnitAction.eUnitAction.MoveRight:
                                AddMoveAcceleration(entity, MoveAcceleration.Direction.Right, act.parameter);
                                break;
                            case UnitAction.eUnitAction.StopMoveRight:
                                AddMoveAcceleration(entity, MoveAcceleration.Direction.Right, -act.parameter);
                                break;
                            case UnitAction.eUnitAction.AutoStopMove:
                                {
                                    if ( !EntityManager.HasComponent<StopMovement>(entity) )
                                    {
                                        PostUpdateCommands.AddComponent<StopMovement>(entity);
                                    }
                                    // no UnitAcceleratio, means stopped
                                }
                                break;
                            case UnitAction.eUnitAction.TurnUp:
                                AddRotateAcceleration(entity, Vector3.left, act.parameter);
                                break;
                            case UnitAction.eUnitAction.StopTurnUp:
                                AddRotateAcceleration(entity, Vector3.left, -act.parameter);
                                break;
                            case UnitAction.eUnitAction.TurnRight:
                                AddRotateAcceleration(entity, Vector3.up, act.parameter);
                                break;
                            case UnitAction.eUnitAction.StopTurnRight:
                                AddRotateAcceleration(entity, Vector3.up, -act.parameter);
                                break;
                            case UnitAction.eUnitAction.AutoStopTurn:
                                {
                                    if (!EntityManager.HasComponent<StopRotation>(entity))
                                    {
                                        PostUpdateCommands.AddComponent<StopRotation>(entity);
                                    }
                                }
                                break;
                        }
                    }

                    actions.Clear();
                }
            );
        }

        private void AddMoveAcceleration(Entity entity,
            MoveAcceleration.Direction eMoveDir, float parameter)
        {
            if (EntityManager.HasComponent<MoveAbility>(entity))
            {
                var moveAbility = EntityManager.GetComponentData<MoveAbility>(entity);

                if (EntityManager.HasComponent<MoveAcceleration>(entity))
                {
                    var accComp = EntityManager.GetComponentData<MoveAcceleration>(entity);
                    switch( eMoveDir )
                    {
                        case MoveAcceleration.Direction.Forward:
                            accComp.forwardSpeed += parameter * moveAbility.linearAcceleration;
                            break;
                        case MoveAcceleration.Direction.Right:
                            accComp.rightSpeed += parameter * moveAbility.linearAcceleration;
                            break;
                        case MoveAcceleration.Direction.Up:
                            accComp.upSpeed += parameter * moveAbility.linearAcceleration;
                            break;
                    }
                    EntityManager.SetComponentData(entity, accComp);
                }
                else
                {
                    var accComp = new MoveAcceleration();
                    switch (eMoveDir)
                    {
                        case MoveAcceleration.Direction.Forward:
                            accComp.forwardSpeed = parameter * moveAbility.linearAcceleration;
                            break;
                        case MoveAcceleration.Direction.Right:
                            accComp.rightSpeed = parameter * moveAbility.linearAcceleration;
                            break;
                        case MoveAcceleration.Direction.Up:
                            accComp.upSpeed = parameter * moveAbility.linearAcceleration;
                            break;
                    }
                    PostUpdateCommands.AddComponent<MoveAcceleration>(entity, accComp);
                }

                if (EntityManager.HasComponent<StopMovement>(entity))
                {
                    PostUpdateCommands.RemoveComponent<StopMovement>(entity);
                }
            }
        }

        private void AddRotateAcceleration(Entity entity, float3 rotateAxis, float parameter)
        {
            if (EntityManager.HasComponent<MoveAbility>(entity))
            {
                var moveAbility = EntityManager.GetComponentData<MoveAbility>(entity);
                var angularAcc = rotateAxis * parameter;

                if (EntityManager.HasComponent<RotateAcceleration>(entity))
                {
                    var accComp = EntityManager.GetComponentData<RotateAcceleration>(entity);
                    accComp.angular += angularAcc;
                    EntityManager.SetComponentData(entity, accComp);
                }
                else
                {
                    var acceleration = new RotateAcceleration { angular = angularAcc };
                    PostUpdateCommands.AddComponent<RotateAcceleration>(entity, acceleration);
                }

                if (EntityManager.HasComponent<StopRotation>(entity))
                {
                    PostUpdateCommands.RemoveComponent<StopRotation>(entity);
                }
            }
        }
    }
}

