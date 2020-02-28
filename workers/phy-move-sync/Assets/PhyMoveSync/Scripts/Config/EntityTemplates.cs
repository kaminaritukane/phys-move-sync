using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.StandardTypes;
using UnityEngine;

namespace PhyMoveSync.Scripts.Config
{
    public static class EntityTemplates
    {
        public static EntityTemplate CreatePlayerEntityTemplate(string workerId, byte[] serializedArguments)
        {
            var clientAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            var serverAttribute = UnityGameLogicConnector.WorkerType;

            var template = new EntityTemplate();

            var position = new Vector3
            {
                x = Random.Range(-20f, 20f),
                y = Random.Range(-5f, 5f),
                z = Random.Range(-20f, 20f)
            };

            template.AddComponent(new Position.Snapshot(position.ToSpatialCoordinates()), serverAttribute);
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
