using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class RollbackServiceTask : ElasticsearchInstallationTaskBase
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public RollbackServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Elasticsearch");
		}

		public RollbackServiceTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			var seesService = this.ServiceStateProvider.SeesService;
			this.Session.Log($"Trying to execute StopServiceTask seeing service: " + seesService);
			if (!seesService) return true;
			var totalTicks = 1000;
			this.Session.SendActionStart(totalTicks, ActionName, "Stopping existing Elasticsearch service", "Elasticsearch service: [1]");
			this.ServiceStateProvider.StopIfRunning(TimeSpan.FromSeconds(60));
			this.Session.SendProgress(1000, "stopped");
			return true;
		}
	}
}
