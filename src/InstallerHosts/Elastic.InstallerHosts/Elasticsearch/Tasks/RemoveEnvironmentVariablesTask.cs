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
				this.Session.Log($"{nameof(RemoveEnvironmentVariablesTask)}: Already installed and rolling back");
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