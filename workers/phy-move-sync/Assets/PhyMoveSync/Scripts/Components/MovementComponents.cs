using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhyMoveSync
{
    public struct MoveAbility : IComponentData
    {
        public float linearAcceleration;
        public float angularAcceleration;
    }

    public struct MoveAcceleration : IComponentData
    {
        public enum Direction
        {
            Forward,
            Right,
            Up
        }

        public float forwardSpeed;
        public float rightSpeed;
        public float upSpeed;

        public float3 localLinear
        {
            get
            {
                var fwd = Vector3.forward * forwardSpeed;
                var rht = Vector3.right * rightSpeed;
                var up  = Vector3.up * upSpeed;
                return fwd + rht + up;
            }
        }
    }

    public struct StopMovement : IComponentData
    {
    }

    public struct RotateAcceleration : IComponentData
    {
        public float3 angular;
    }

    public struct StopRotation : IComponentData
    {
    }

    public struct InputReceiver : IComponentData
    {
        public bool hasMoveInput;
        public bool hasRotateInput;
    }

    [InternalBufferCapacity(16)]
    public struct UnitAction : IBufferElementData
    {
        public enum eUnitAction
        {
            MoveForward,
            StopMoveForward,
            MoveRight,
            StopMoveRight,
            AutoStopMove,
            TurnUp,
            StopTurnUp,
            TurnRight,
            StopTurnRight,
            AutoStopTurn
        }

        public eUnitAction action;
        public float parameter;
    }
}