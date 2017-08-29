using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class RemoveEnvironmentVariablesTask : ElasticsearchInstallationTask
	{
		public RemoveEnvironmentVariablesTask(string[] args, ISession session) : base(args, session) { }
		public RemoveEnvironmentVariablesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var esState = this.InstallationModel.ElasticsearchEnvironmentConfiguration;

			if (this.InstallationModel.NoticeModel.ExistingVersionInstalled)
			{
				if (!this.Session.IsUninstalling)
				{
					// handle rolling back to a version that uses the old config environment variable
					if (this.Session.IsRollback && this.InstallationModel.ElasticsearchEnvironmentConfiguration.RestoreOldConfigVariable())
						esState.SetEsConfigEnvironmentVariable(null);

					this.Session.Log($"Skipping {nameof(RemoveEnvironmentVariablesTask)}: Already installed and not currently uninstalling");
					return true;
				}

				if (!this.Session.IsRollback)
				{
					this.Session.Log($"Skipping {nameof(RemoveEnvironmentVariablesTask)}: Already installed and not currently rolling back");
					return true;
				}
			}

			this.Session.SendActionStart(1000, ActionName, "Removing environment variables", "[1]");
			esState.SetEsHomeEnvironmentVariable(null);
			esState.SetEsConfigEnvironmentVariable(null);
			this.Session.SendProgress(1000, "Environment variables removed");
			return true;
		}
	}
}