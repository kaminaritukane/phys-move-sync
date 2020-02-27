using Improbable.Gdk.StandardTypes;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhyMoveSync
{
    public class SystemCreator : MonoBehaviour
    {
        private void Start()
        {
            var compSysGroup = World.Active.GetExistingSystem<SimulationSystemGroup>();

            var inputSys = World.Active.GetOrCreateSystem<InputSystem>();
            var actionSys = World.Active.GetOrCreateSystem<UnitActionSystem>();
            var moveSys = World.Active.GetOrCreateSystem<PhysicsMoveSystem>();

            compSysGroup.AddSystemToUpdateList(inputSys);
            compSysGroup.AddSystemToUpdateList(actionSys);
            compSysGroup.AddSystemToUpdateList(moveSys);

            //quaternion q = Quaternion.Euler(0, 0, 45);
            //var ui = q.ToCompressedQuaternion();
            //var qq = ui.ToUnityQuaternion();

            //Debug.Log($"q:{q}, ui:{ui.Data}, qq:{qq}");

            //var radians = Vector3Extensions.Radians(new float3(1, 0, 0), new float3(-1, 0, 0));
            //Debug.Log($"radians: {radians}, angle: {radians * 180f / math.PI}");
        }
    }
}