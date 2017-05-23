using Elastic.Installer.Domain.Model.Kibana;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Kibana.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Kibana.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Rollback
{
	public class KibanaRollbackServiceAction : RollbackCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaRollbackServiceAction);
		public override int Order => (int)KibanaCustomActionOrder.RollbackService;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(KibanaServiceInstallAction));

		[CustomAction]
		public static ActionResult KibanaRollbackService(Session session) =>
			session.Handle(() => new UninstallServiceTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
