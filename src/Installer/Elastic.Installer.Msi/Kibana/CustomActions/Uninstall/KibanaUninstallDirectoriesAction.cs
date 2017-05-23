using Elastic.Installer.Domain.Model.Kibana;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Kibana.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Uninstall
{
	public class KibanaUninstallDirectoriesAction : UninstallCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaUninstallDirectoriesAction);
		public override int Order => (int)KibanaCustomActionOrder.UninstallDirectories;
		public override Step Step => new Step(nameof(KibanaUninstallServiceAction));
		public override When When => When.After;
		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");
		
		[CustomAction]
		public static ActionResult KibanaUninstallDirectories(Session session) =>
			session.Handle(() => new DeleteDirectoriesTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
