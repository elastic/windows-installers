using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Kibana;

namespace Elastic.InstallerHosts.Kibana.Tasks
{
	public class RemoveEnvironmentVariablesTask : KibanaInstallationTask
	{
		public RemoveEnvironmentVariablesTask(string[] args, ISession session) : base(args, session) { }
		public RemoveEnvironmentVariablesTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			if (this.InstallationModel.NoticeModel.AlreadyInstalled && !this.Session.Uninstalling)
				return true;

			this.Session.SendActionStart(1000, ActionName, "Removing environment variables", "[1]");

			var esState = this.InstallationModel.KibanaEnvironmentState;
			esState.SetKibanaHomeEnvironmentVariable(null);
			esState.SetKibanaConfigEnvironmentVariable(null);
			this.Session.SendProgress(1000, "Environment variables removed");
			return true;
		}
	}
}