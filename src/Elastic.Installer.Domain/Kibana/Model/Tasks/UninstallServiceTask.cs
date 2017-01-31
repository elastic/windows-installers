using System.IO.Abstractions;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Kibana.Model.Tasks
{
	public class UninstallServiceTask : KibanaInstallationTask
	{
		private IServiceStateProvider ServiceStateProvider{ get; }

		public UninstallServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Kibana");
		}

		public UninstallServiceTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			if (!this.ServiceStateProvider.SeesService)
			{
				this.Session.Log("No service running with the name 'Kibana'");
				return true;
			}

			if (this.InstallationModel.NoticeModel.AlreadyInstalled)
			{
				this.Session.Log("Rolling back from a previous installation so leaving service alone");
				return true;

			}
			this.Session.SendActionStart(2000, ActionName, "Uninstalling Kibana service", "Kibana service: [1]");
			this.Session.SendProgress(1000, "uninstalling");
			this.ServiceStateProvider.RunTimeUninstall(this.InstallationModel.GetServiceConfiguration());
			this.Session.SendProgress(1000, "uninstalled");

			return true;
		}
	}
}