using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class InstallServiceTask : InstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public InstallServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session);
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