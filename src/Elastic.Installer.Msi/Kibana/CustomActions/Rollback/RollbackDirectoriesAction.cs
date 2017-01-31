using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Kibana.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Kibana.CustomActions.Install;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Rollback
{
	public class KibanaRollbackDirectoriesAction : RollbackCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaRollbackDirectoriesAction);
		public override int Order => (int)KibanaCustomActionOrder.RollbackDirectories;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(KibanaInstallDirectoriesAction));

		[CustomAction("KibanaRollbackDirectories")]
		public static ActionResult KibanaRollbackDirectories(Session session) =>
			session.Handle(() => new DeleteDirectoriesTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
