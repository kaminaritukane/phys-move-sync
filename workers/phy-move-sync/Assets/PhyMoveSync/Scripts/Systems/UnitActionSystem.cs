using Improbable.Gdk.StandardTypes;
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
            if (EntityManager.HasComponent<UnitMoveAbility.Component>(entity))
            {
                var moveAbility = EntityManager.GetComponentData<UnitMoveAbility.Component>(entity);
                var linearAcc = moveAbility.LinearAcceleration.ToFloat10k();

                if (EntityManager.HasComponent<MoveAcceleration>(entity))
                {
                    var accComp = EntityManager.GetComponentData<MoveAcceleration>(entity);
                    switch( eMoveDir )
                    {
                        case MoveAcceleration.Direction.Forward:
                            accComp.forwardAcc += parameter * linearAcc;
                            break;
                        case MoveAcceleration.Direction.Right:
                            accComp.rightAcc += parameter * linearAcc;
                            break;
                        case MoveAcceleration.Direction.Up:
                            accComp.upAcc += parameter * linearAcc;
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
                            accComp.forwardAcc = parameter * linearAcc;
                            break;
                        case MoveAcceleration.Direction.Right:
                            accComp.rightAcc = parameter * linearAcc;
                            break;
                        case MoveAcceleration.Direction.Up:
                            accComp.upAcc = parameter * linearAcc;
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
            if (EntityManager.HasComponent<UnitMoveAbility.Component>(entity))
            {
                var moveAbility = EntityManager.GetComponentData<UnitMoveAbility.Component>(entity);
                var angularAccAbility = moveAbility.AngularAcceleration.ToFloat10k();
                var angularAcc = rotateAxis * parameter * angularAccAbility;

                if (EntityManager.HasComponent<RotateAcceleration>(entity))
                {
                    var accComp = EntityManager.GetComponentData<RotateAcceleration>(entity);
                    accComp.angularAcc += angularAcc;
                    EntityManager.SetComponentData(entity, accComp);
                }
                else
                {
                    var acceleration = new RotateAcceleration { angularAcc = angularAcc };
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

