using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhyMoveSync
{
    public struct MoveAcceleration : IComponentData
    {
        public enum Direction
        {
            Forward,
            Right,
            Up
        }

        public float forwardAcc;
        public float rightAcc;
        public float upAcc;

        public float3 localLinear
        {
            get
            {
                var fwd = Vector3.forward * forwardAcc;
                var rht = Vector3.right * rightAcc;
                var up  = Vector3.up * upAcc;
                return fwd + rht + up;
            }
        }
    }

    public struct StopMovement : IComponentData
    {
    }

    public struct RotateAcceleration : IComponentData
    {
        public float3 angularAcc;
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