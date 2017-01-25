using System.IO.Abstractions;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class UninstallServiceTask : ElasticsearchInstallationTask
	{
		private IServiceStateProvider ServiceStateProvider{ get; }

		public UninstallServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Elasticsearch");
		}

		public UninstallServiceTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			if (!this.ServiceStateProvider.SeesService)
			{
				this.Session.Log("No service running with the name 'Elasticsearch'");
				return true;
			}

			if (this.InstallationModel.NoticeModel.AlreadyInstalled)
			{
				this.Session.Log("Rolling back from a previous installation so leaving service alone");
				return true;

			}
			this.Session.SendActionStart(2000, ActionName, "Uninstalling Elasticsearch service", "Elasticsearch service: [1]");
			this.Session.SendProgress(1000, "uninstalling");
			this.ServiceStateProvider.RunTimeUninstall(this.InstallationModel.GetServiceConfiguration());
			this.Session.SendProgress(1000, "uninstalled");

			return true;
		}
	}
}