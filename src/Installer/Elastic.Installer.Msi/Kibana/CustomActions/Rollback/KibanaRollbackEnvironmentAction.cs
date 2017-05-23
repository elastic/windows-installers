using Elastic.Installer.Domain.Model.Kibana;
using Elastic.Installer.Msi.CustomActions;
using Elastic.Installer.Msi.Kibana.CustomActions.Install;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Kibana.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Rollback
{
	public class KibanaRollbackEnvironmentAction : RollbackCustomAction<Kibana>
	{
		public override string Name => nameof(KibanaRollbackEnvironmentAction);
		public override int Order => (int)KibanaCustomActionOrder.RollbackEnvironment;
		public override When When => When.Before;
		public override Step Step => new Step(nameof(KibanaEnvironmentAction));

		[CustomAction]
		public static ActionResult KibanaRollbackEnvironment(Session session) =>
			session.Handle(() => new RemoveEnvironmentVariablesTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
