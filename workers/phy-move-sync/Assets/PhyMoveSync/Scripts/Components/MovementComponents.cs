using Unity.Entities;
using Unity.Mathematics;

namespace PhyMoveSync
{
    public struct MoveAbility : IComponentData
    {
        public float linearAcceleration;
        public float angularAcceleration;
    }

    public struct MoveAcceleration : IComponentData
    {
        public float3 linear;
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