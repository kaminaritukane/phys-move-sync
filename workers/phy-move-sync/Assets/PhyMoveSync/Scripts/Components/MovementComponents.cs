using Unity.Entities;
using Unity.Mathematics;

namespace PhyMoveSync
{
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

    [InternalBufferCapacity(8)]
    public struct UnitAction : IBufferElementData
    {
        public enum eUnitAction
        {
            MoveForward,
            MoveBackward,
            MoveLeft,
            MoveRight,
            StopMove,
            TurnLeft,
            TurnRight,
            StopTurn
        }

        public eUnitAction action;
    }
}