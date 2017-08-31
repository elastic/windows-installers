using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
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
			if (this.Session.IsRollback && this.InstallationModel.NoticeModel.ExistingVersionInstalled)
			{
				// handle rolling back to a version that uses the old config environment variable
				this.Session.Log($"Skipping {nameof(UninstallServiceTask)}: Already installed and rolling back");
				return true;
			}

			if (this.InstallationModel.NoticeModel.ExistingVersionInstalled && !this.Session.IsUninstalling)
			{
				this.Session.Log($"Skipping {nameof(UninstallServiceTask)}: Already installed and not currently uninstalling");
				return true;
			}

			if (!this.ServiceStateProvider.SeesService)
			{
				this.Session.Log("No service running with the name 'Elasticsearch'");
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