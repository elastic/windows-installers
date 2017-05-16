using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Kibana.Model.Tasks;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Uninstall
{
	public class KibanaUninstallServiceAction : UninstallCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaUninstallServiceAction);
		public override int Order => (int)KibanaCustomActionOrder.UninstallService;
		public override Step Step => new Step(nameof(KibanaUninstallPluginsAction));
		public override When When => When.After;

		public override Condition Condition => new Condition("(NOT UPGRADINGPRODUCTCODE) AND (REMOVE=\"ALL\")");

		[CustomAction]
		public static ActionResult KibanaUninstallService(Session session) =>
			session.Handle(() => new UninstallServiceTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
