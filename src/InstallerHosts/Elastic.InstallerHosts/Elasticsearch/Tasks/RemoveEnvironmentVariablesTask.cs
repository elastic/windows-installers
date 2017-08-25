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
			if (this.InstallationModel.NoticeModel.AlreadyInstalled && !this.Session.IsUninstalling)
				return true;

			this.Session.SendActionStart(1000, ActionName, "Removing environment variables", "[1]");

			var esState = this.InstallationModel.ElasticsearchEnvironmentConfiguration;
			esState.SetEsHomeEnvironmentVariable(null);
			esState.SetEsConfigEnvironmentVariable(null);
			this.Session.SendProgress(1000, "Environment variables removed");
			return true;
		}
	}
}