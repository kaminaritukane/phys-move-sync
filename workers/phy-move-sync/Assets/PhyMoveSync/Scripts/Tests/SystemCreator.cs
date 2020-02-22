using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
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
        }
    }
}