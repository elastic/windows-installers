using System;
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
			{
				try
				{
					this.FileSystem.Directory.Delete(this.TempDirectory, true);
				}
				catch (Exception e)
				{
					// log, but continue.
					this.Session.Log($"Exception deleting {this.TempDirectory}: {e}");
				}
			}

			return true;
		}
	}
}