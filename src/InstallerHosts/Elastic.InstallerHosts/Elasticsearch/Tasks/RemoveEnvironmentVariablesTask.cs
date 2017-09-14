using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class RemoveEnvironmentVariablesTask : ElasticsearchInstallationTaskBase
	{
		public RemoveEnvironmentVariablesTask(string[] args, ISession session) : base(args, session) { }
		public RemoveEnvironmentVariablesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var esState = this.InstallationModel.ElasticsearchEnvironmentConfiguration;

			if (this.Session.IsRollback && this.InstallationModel.NoticeModel.ExistingVersionInstalled)
			{
				this.Session.Log($"{nameof(RemoveEnvironmentVariablesTask)}: Rolling back and checking for existence of ES_CONFIG_OLD");

				// handle rolling back to a version that uses the old config environment variable
				if (this.InstallationModel.ElasticsearchEnvironmentConfiguration.RestoreOldConfigVariable())
				{
					this.Session.Log($"Skipping {nameof(RemoveEnvironmentVariablesTask)}: ES_CONFIG_OLD found and reinstated");
					esState.SetEsConfigEnvironmentVariable(null);
				}

				return true;
			}

			if (this.InstallationModel.NoticeModel.ExistingVersionInstalled && !this.Session.IsUninstalling)
			{
				this.Session.Log($"Skipping {nameof(RemoveEnvironmentVariablesTask)}: Already installed and not currently uninstalling");
				return true;
			}

			this.Session.SendActionStart(1000, ActionName, "Removing environment variables", "[1]");
			esState.SetEsHomeEnvironmentVariable(null);
			esState.SetEsConfigEnvironmentVariable(null);
			this.Session.SendProgress(1000, "Environment variables removed");
			return true;
		}
	}
}