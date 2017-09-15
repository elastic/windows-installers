using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Commit
{
	public class CleanupInstallTask : ElasticsearchInstallationTaskBase
	{
		public CleanupInstallTask(string[] args, ISession session) : base(args, session) {}

		public CleanupInstallTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		protected override bool ExecuteTask()
		{
			this.InstallationModel.TempDirectoryConfiguration.CleanUp();
			return true;
		}
	}
}