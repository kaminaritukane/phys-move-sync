using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace PhyMoveSync
{
    [DisableAutoCreation]
    public class InputSystem : ComponentSystem
    {
        private EntityQuery receiverQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            receiverQuery = GetEntityQuery(
                ComponentType.ReadOnly<InputReceiver>()
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(receiverQuery).ForEach(
                ( Entity entity,
                  ref InputReceiver iptReceiver ) => 
                {
                    var actions = EntityManager.GetBuffer<UnitAction>(entity);
                    CheckMoveInputs(actions, ref iptReceiver);
                    CheckRotateInputs(actions, ref iptReceiver);
                }
            );
        }

        private void CheckMoveInputs(DynamicBuffer<UnitAction> actions, ref InputReceiver iptReceiver)
        {
            // forward
            if (Input.GetKeyDown(KeyCode.W))
            {
                actions.Add(new UnitAction {
                    action = UnitAction.eUnitAction.MoveForward,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: MoveForward 1");
            }
            else if ( Input.GetKeyUp(KeyCode.W))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveForward,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: StopMoveForward 1");
            }

            // backward
            if (Input.GetKeyDown(KeyCode.S))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.MoveForward,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action: MoveForward -1");
            }
            else if (Input.GetKeyUp(KeyCode.S))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveForward,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action: StopMoveForward -1");
            }

            // right
            if (Input.GetKeyDown(KeyCode.D))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.MoveRight,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: MoveRight 1");
            }
            else if (Input.GetKeyUp(KeyCode.D))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveRight,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: StopMoveRight 1");
            }

            // left
            if (Input.GetKeyDown(KeyCode.A))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.MoveRight,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action: MoveRight -1");
            }
            else if (Input.GetKeyUp(KeyCode.A))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveRight,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action: StopMoveRight -1");
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.AutoStopMove
                });
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.AutoStopTurn
                });
                iptReceiver.hasMoveInput = false;
                iptReceiver.hasRotateInput = false;
            }
            else
            {
                // auto stop move
                var hasMoveInput = Input.GetKey(KeyCode.W)
                    || Input.GetKey(KeyCode.S)
                    || Input.GetKey(KeyCode.A)
                    || Input.GetKey(KeyCode.D);
                if (iptReceiver.hasMoveInput && !hasMoveInput)
                {
                    actions.Add(new UnitAction { action = UnitAction.eUnitAction.AutoStopMove });
                    //Debug.Log("Add Action: AutoStopMove");
                }

                iptReceiver.hasMoveInput = hasMoveInput;
            }
        }

        private void CheckRotateInputs(DynamicBuffer<UnitAction> actions, ref InputReceiver iptReceiver)
        {
            // TurnUp
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.TurnUp,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: TurnUp 1");
            }
            else if (Input.GetKeyUp(KeyCode.UpArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopTurnUp,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: StopTurnUp 1");
            }

            // TurnDown
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.TurnUp,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action TurnUp -1");
            }
            else if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopTurnUp,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action: StopTurnUp -1");
            }

            // TurnRight
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.TurnRight,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: TurnRight 1");
            }
            else if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopTurnRight,
                    parameter = 1.0f
                });
                //Debug.Log("Add Action: StopTurnRight 1");
            }

            // TurnLeft
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.TurnRight,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action: TurnRight -1");
            }
            else if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopTurnRight,
                    parameter = -1.0f
                });
                //Debug.Log("Add Action: StopTurnRight -1");
            }

            // auto stop turn
            var hasRotateInput = Input.GetKey(KeyCode.UpArrow)
                || Input.GetKey(KeyCode.DownArrow)
                || Input.GetKey(KeyCode.LeftArrow)
                || Input.GetKey(KeyCode.RightArrow);
            if (iptReceiver.hasRotateInput && !hasRotateInput)
            {
                actions.Add(new UnitAction { action = UnitAction.eUnitAction.AutoStopTurn });
                //Debug.Log("Add Action: AutoStopTurn");
            }

            iptReceiver.hasRotateInput = hasRotateInput;
        }
    }
}