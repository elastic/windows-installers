using Elastic.Installer.Domain.Model.Kibana;
using Elastic.Installer.Msi.CustomActions;
using Elastic.InstallerHosts;
using Elastic.InstallerHosts.Kibana.Tasks;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions
{
	public class KibanaValidateArgumentsAction : CustomAction<Kibana>
	{
		public override string Name => nameof(KibanaValidateArgumentsAction);
		public override int Order => (int)KibanaCustomActionOrder.LogAllTheThings;
		public override Condition Condition => Condition.Always;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override Step Step => Step.InstallInitialize;
		public override When When => When.After;
		public override Execute Execute => Execute.immediate;

		[CustomAction("KibanaValidateArguments")]
		public static ActionResult KibanaValidateArguments(Session session) =>
			session.Handle(() => new ValidateArgumentsTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}