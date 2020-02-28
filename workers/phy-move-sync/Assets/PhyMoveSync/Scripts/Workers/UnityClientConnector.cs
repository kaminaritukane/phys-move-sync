using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using System;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace PhyMoveSync
{
    public class UnityClientConnector : WorkerConnector
    {
        public const string WorkerType = "UnityClient";

        private async void Start()
        {
            var connParams = CreateConnectionParameters(WorkerType);

            var builder = new SpatialOSConnectionHandlerBuilder()
                .SetConnectionParameters(connParams);

            if (!Application.isEditor)
            {
                var initializer = new CommandLineConnectionFlowInitializer();
                switch (initializer.GetConnectionService())
                {
                    case ConnectionService.Receptionist:
                        builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType), initializer));
                        break;
                    case ConnectionService.Locator:
                        builder.SetConnectionFlow(new LocatorFlow(initializer));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                builder.SetConnectionFlow(new ReceptionistFlow(CreateNewWorkerId(WorkerType)));
            }

            await Connect(builder, new ForwardingDispatcher()).ConfigureAwait(false);
        }

        protected override void HandleWorkerConnectionEstablished()
        {
            Worker.World.CreateSystem<ClientUnitCreatSystem>(transform.position);

            Worker.World.GetOrCreateSystem<InputSystem>();
            Worker.World.GetOrCreateSystem<UnitActionSystem>();
            Worker.World.GetOrCreateSystem<PhysicsMoveSystem>();
            Worker.World.GetOrCreateSystem<PhysicsCollisionSystem>();

            Worker.World.GetOrCreateSystem<BuildPhysicsWorld>();
            Worker.World.GetOrCreateSystem<StepPhysicsWorld>();
            Worker.World.GetOrCreateSystem<ExportPhysicsWorld>();
            Worker.World.GetOrCreateSystem<EndFramePhysicsSystem>();

            Worker.World.GetOrCreateSystem<ClientMovementSyncSystem>();

            // for render
            Worker.World.GetOrCreateSystem<EndFrameTRSToLocalToWorldSystem>();
            Worker.World.GetOrCreateSystem<CreateMissingRenderBoundsFromMeshRenderer>();
            Worker.World.GetOrCreateSystem<RenderBoundsUpdateSystem>();
            Worker.World.GetOrCreateSystem<RenderMeshSystemV2>();

            PlayerLifecycleHelper.AddClientSystems(Worker.World);
        }
    }
}
