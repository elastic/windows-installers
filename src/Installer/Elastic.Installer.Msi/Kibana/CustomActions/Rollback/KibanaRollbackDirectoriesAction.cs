using Elastic.Installer.Domain.Model.Kibana;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Kibana.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Kibana.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Rollback
{
	public class KibanaRollbackDirectoriesAction : RollbackCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaRollbackDirectoriesAction);
		public override int Order => (int)KibanaCustomActionOrder.RollbackDirectories;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(KibanaDirectoriesAction));

		[CustomAction]
		public static ActionResult KibanaRollbackDirectories(Session session) =>
			session.Handle(() => new DeleteDirectoriesTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
