using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class InstallServiceTask : ElasticsearchInstallationTaskBase
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public InstallServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Elasticsearch");
		}

		public InstallServiceTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			if (!this.InstallationModel.ServiceModel.InstallAsService)
				return true;

			var totalTicks = this.InstallationModel.ServiceModel.StartAfterInstall ? 4000 : 2000;

			this.Session.SendActionStart(totalTicks, ActionName, "Installing Elasticsearch service", "Elasticsearch service: [1]");

			var config = this.InstallationModel.GetServiceConfiguration();
			this.Session.Log("Service Configuration:\r\n" + config);
			this.Session.SendProgress(1000, "installing");

			this.ServiceStateProvider.RunTimeInstall(config);
			this.Session.SendProgress(1000, "installed");
		
			return true;
		}
	}
}