using Elastic.Installer.Domain.Session;
using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class StartServiceTask : InstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public StartServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session);
		}

		public StartServiceTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			this.Session.Log("Executing start service!");
			if (!this.InstallationModel.ServiceModel.StartAfterInstall)
				return true;
			var seesService = this.ServiceStateProvider.SeesService;
			this.Session.Log($"Trying to execute StartServiceTask seeing service: " + seesService);
			if (!seesService) return true;
			var totalTicks = 1000;
			this.Session.SendActionStart(totalTicks, ActionName, "Starting Elasticsearch service");
			this.ServiceStateProvider.StartAndWaitForRunning(TimeSpan.FromSeconds(60), 2000);
			this.Session.SendProgress(1000, "Elasticsearch service started");
			return true;
		}
	}
}
