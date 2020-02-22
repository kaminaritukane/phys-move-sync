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
                Debug.Log("Add Action MoveForward 1");
            }
            else if ( Input.GetKeyUp(KeyCode.W))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveForward,
                    parameter = 1.0f
                });
                Debug.Log("Add StopMoveForward 1");
            }

            // backward
            if (Input.GetKeyDown(KeyCode.S))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.MoveForward,
                    parameter = -1.0f
                });
                Debug.Log("Add Action MoveForward -1");
            }
            else if (Input.GetKeyUp(KeyCode.S))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveForward,
                    parameter = -1.0f
                });
                Debug.Log("Add StopMoveForward -1");
            }

            // right
            if (Input.GetKeyDown(KeyCode.D))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.MoveRight,
                    parameter = 1.0f
                });
                Debug.Log("Add Action MoveRight 1");
            }
            else if (Input.GetKeyUp(KeyCode.D))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveRight,
                    parameter = 1.0f
                });
                Debug.Log("Add StopMoveRight 1");
            }

            // left
            if (Input.GetKeyDown(KeyCode.A))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.MoveRight,
                    parameter = -1.0f
                });
                Debug.Log("Add Action MoveRight -1");
            }
            else if (Input.GetKeyUp(KeyCode.A))
            {
                actions.Add(new UnitAction
                {
                    action = UnitAction.eUnitAction.StopMoveRight,
                    parameter = -1.0f
                });
                Debug.Log("Add StopMoveRight -1");
            }

            var hasMoveInput = Input.GetKey(KeyCode.W)
                || Input.GetKey(KeyCode.S)
                || Input.GetKey(KeyCode.A)
                || Input.GetKey(KeyCode.D);
            if ( iptReceiver.hasMoveInput && !hasMoveInput )
            {
                actions.Add(new UnitAction { action = UnitAction.eUnitAction.AutoStopMove });
                Debug.Log("Add AutoStopMove");
            }

            iptReceiver.hasMoveInput = hasMoveInput;
        }
    }
}