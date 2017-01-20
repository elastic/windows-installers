using Elastic.Installer.Domain.Kibana.Model.Tasks;
using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using Microsoft.Deployment.WindowsInstaller;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana.CustomActions.Install
{
	public class KibanaInstallDirectoriesAction : CustomAction<Kibana>
	{
		public override string Name => nameof(KibanaInstallDirectoriesAction);
		public override int Order => (int)KibanaCustomActionOrder.InstallDirectories;
		public override Condition Condition => Condition.NOT_Installed;
		public override Return Return => Return.check;
		public override Sequence Sequence => Sequence.InstallExecuteSequence;
		public override When When => When.After;
		public override Step Step => Step.InstallFiles;
		public override Execute Execute => Execute.deferred;

		[CustomAction]
		public static ActionResult KibanaInstallDirectories(Session session) =>
			session.Handle(() => new CreateDirectoriesTask(session.ToSetupArguments(KibanaArgumentParser.AllArguments), session.ToISession()).Execute());
	}
}
