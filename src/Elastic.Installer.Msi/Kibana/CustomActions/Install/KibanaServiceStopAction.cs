using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;
using Elastic.Installer.Domain.Kibana.Model.Tasks;
using Elastic.Installer.Domain.Kibana.Model;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Install
{
	public class KibanaServiceStopAction : CustomAction<Kibana>
	{
		public override string Name => nameof(KibanaServiceStopAction);
		public override int Order => (int)KibanaCustomActionOrder.InstallStopService;
		public override Condition Condition => Condition.Always;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;

		// Stop the service before the "FilesInUse" dialog can be shown
		// in InstallValidate
		public override When When => When.Before;
		public override Step Step => Step.InstallValidate;
		public override Execute Execute => Execute.immediate;

		[CustomAction]
		public static ActionResult KibanaServiceStop(Session session) =>
			session.Handle(() => new StopServiceTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
