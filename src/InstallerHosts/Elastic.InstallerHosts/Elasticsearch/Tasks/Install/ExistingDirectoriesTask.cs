using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	/// <summary>
	/// Sets properties that can be used to determine if data,logs and config directories already exist.
	/// </summary>
	public class ExistingDirectoriesTask : ElasticsearchInstallationTaskBase
	{
		public ExistingDirectoriesTask(string[] args, ISession session)
			: base(args, session)
		{
		}

		public ExistingDirectoriesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem)
		{
		}

		protected override bool ExecuteTask()
		{
			Session.Set("ConfigDirectoryExists",
				this.FileSystem.Directory.Exists(this.InstallationModel.LocationsModel.ConfigDirectory).ToString());
			Session.Set("LogsDirectoryExists",
				this.FileSystem.Directory.Exists(this.InstallationModel.LocationsModel.LogsDirectory).ToString());
			Session.Set("DataDirectoryExists",
				this.FileSystem.Directory.Exists(this.InstallationModel.LocationsModel.DataDirectory).ToString());
			return true;
		}
	}
}
