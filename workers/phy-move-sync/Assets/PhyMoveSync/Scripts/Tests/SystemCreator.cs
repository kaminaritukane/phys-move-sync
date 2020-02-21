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
            var moveSys = World.Active.GetOrCreateSystem<PhysicsMoveSystem>();
            compSysGroup.AddSystemToUpdateList(moveSys);
        }
    }
}