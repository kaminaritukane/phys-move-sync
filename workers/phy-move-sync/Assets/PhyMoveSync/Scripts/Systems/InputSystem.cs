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
            bool hasMoveInput = false;

            // move forward/backward
            if (Input.GetKey(KeyCode.W))
            {
                actions.Add(new UnitAction { action = UnitAction.eUnitAction.MoveForward });
                hasMoveInput = true;
            }
            else if (Input.GetKey(KeyCode.S))
            {
                actions.Add(new UnitAction { action = UnitAction.eUnitAction.MoveBackward });
                hasMoveInput = true;
            }

            // move left/right
            if (Input.GetKey(KeyCode.A))
            {
                actions.Add(new UnitAction { action = UnitAction.eUnitAction.MoveLeft });
                hasMoveInput = true;
            }
            else if (Input.GetKey(KeyCode.D))
            {
                actions.Add(new UnitAction { action = UnitAction.eUnitAction.MoveRight });
                hasMoveInput = true;
            }

            if ( iptReceiver.hasMoveInput && !hasMoveInput )
            {
                actions.Add(new UnitAction { action = UnitAction.eUnitAction.StopMove });
            }

            iptReceiver.hasMoveInput = hasMoveInput;
        }
    }
}