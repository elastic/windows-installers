using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class StopServiceTask : ElasticsearchInstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public StopServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Elasticsearch");
		}

		public StopServiceTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			if (this.InstallationModel.NoticeModel.ExistingVersionInstalled &&
			    this.InstallationModel.NoticeModel.CurrentVersion < this.InstallationModel.NoticeModel.ExistingVersion && 
				this.Session.IsUninstalling)
			{
				this.Session.Log($"Skipping {nameof(StopServiceTask)}: Newer version installed and uninstalling older version");
				return true;
			}

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
