using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.StandardTypes;

namespace PhyMoveSync.Scripts.Config
{
    public static class EntityTemplates
    {
        public static EntityTemplate CreatePlayerEntityTemplate(string workerId, byte[] serializedArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(), clientAttribute);
            template.AddComponent(new Metadata.Snapshot("Player"), serverAttribute);
            template.AddComponent(new ClientMovement.Snapshot(), clientAttribute);
            template.AddComponent(new ServerMovement.Snapshot(), serverAttribute);
            template.AddComponent(new UnitMoveAbility.Snapshot
            {
                LinearAcceleration = 1f.ToInt10k(),
                AngularAcceleration = 1f.ToInt10k(),
                MaxLinearSpeed = 3f.ToInt10k(),
                MaxAngularSpeed = 2f.ToInt10k()
            }, serverAttribute);

            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, serverAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, serverAttribute);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, serverAttribute);

            return template;
        }
    }
}
