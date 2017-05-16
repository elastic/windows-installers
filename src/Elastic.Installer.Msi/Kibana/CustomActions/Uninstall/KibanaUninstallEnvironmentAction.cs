using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Kibana.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
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
