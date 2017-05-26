using Elastic.Installer.Domain.Model.Kibana;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Kibana.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Uninstall
{
	public class KibanaUninstallEnvironmentAction : UninstallCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaUninstallEnvironmentAction);
		public override int Order => (int)KibanaCustomActionOrder.UninstallEnvironment;
		public override Step Step => new Step(nameof(KibanaUninstallDirectoriesAction));
		public override When When => When.After;

		[CustomAction]
		public static ActionResult KibanaUninstallEnvironment(Session session) =>
			session.Handle(() => new RemoveEnvironmentVariablesTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
