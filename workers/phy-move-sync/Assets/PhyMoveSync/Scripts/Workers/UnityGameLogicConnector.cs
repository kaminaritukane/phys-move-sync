using PhyMoveSync.Scripts.Config;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Rendering;

namespace PhyMoveSync
{
    public class UnityGameLogicConnector : WorkerConnector
    {
        public const string WorkerType = "UnityGameLogic";

        private async void Start()
        {
            PlayerLifecycleConfig.CreatePlayerEntityTemplate = EntityTemplates.CreatePlayerEntityTemplate;

            IConnectionFlow flow;
            ConnectionParameters connectionParameters;

            if (Application.isEditor)
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerType));
                connectionParameters = CreateConnectionParameters(WorkerType);
            }
            else
            {
                flow = new ReceptionistFlow(CreateNewWorkerId(WorkerType),
                    new CommandLineConnectionFlowInitializer());
                connectionParameters = CreateConnectionParameters(WorkerType,
                    new CommandLineConnectionParameterInitializer());
            }

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionFlow(flow)
                .SetConnectionParameters(connectionParameters);

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worker.World.CreateSystem<ServerUnitCreatSystem>(transform.position);

            Worker.World.GetOrCreateSystem<PhysicsMoveSystem>();

            Worker.World.GetOrCreateSystem<BuildPhysicsWorld>();
            Worker.World.GetOrCreateSystem<StepPhysicsWorld>();
            Worker.World.GetOrCreateSystem<ExportPhysicsWorld>();
            Worker.World.GetOrCreateSystem<EndFramePhysicsSystem>();

            // for render
            Worker.World.GetOrCreateSystem<EndFrameTRSToLocalToWorldSystem>();
            Worker.World.GetOrCreateSystem<CreateMissingRenderBoundsFromMeshRenderer>();
            Worker.World.GetOrCreateSystem<RenderBoundsUpdateSystem>();
            Worker.World.GetOrCreateSystem<RenderMeshSystemV2>();

            Worker.World.GetOrCreateSystem<MetricSendSystem>();

            PlayerLifecycleHelper.AddServerSystems(Worker.World);
        }
    }
}
