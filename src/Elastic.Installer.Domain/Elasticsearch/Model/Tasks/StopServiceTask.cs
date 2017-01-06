using Elastic.Installer.Domain.Session;
using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class StopServiceTask : InstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public StopServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session);
		}
		public StopServiceTask(InstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
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
			this.Session.SendActionStart(totalTicks, ActionName, "Stopping existing Elasticsearch service");
			this.ServiceStateProvider.StopIfRunning(TimeSpan.FromSeconds(60));
			this.Session.SendProgress(1000, "Elasticsearch service stopped");
			return true;
		}
	}
}
