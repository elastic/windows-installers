using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class CleanupInstallTask : ElasticsearchInstallationTask
	{
		public CleanupInstallTask(string[] args, ISession session) : base(args, session) {}

		public CleanupInstallTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		protected override bool ExecuteTask()
		{
			if (this.FileSystem.Directory.Exists(this.TempDirectory))
				this.FileSystem.Directory.Delete(this.TempDirectory, true);

			return true;
		}
	}
}