using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class DeleteDirectoriesTask : ElasticsearchInstallationTask
	{
		private bool RollBack { get; }
		public DeleteDirectoriesTask(string[] args, ISession session, bool rollBack) : base(args, session)
		{
			RollBack = rollBack;
		}

		public DeleteDirectoriesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		/// <summary>
		/// This action ensures we remove the Elasticsearch installation directory completely.
		/// We can't rely on the MSI RemoveFiles step to remove directories/files that were registered 
		/// on a full uninstall because new files may have been introduced later on (e.g. plugins).
		/// </summary>
		protected override bool ExecuteTask()
		{
			if (this.InstallationModel.NoticeModel.AlreadyInstalled && !this.Session.Uninstalling)
				return true;

			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;

			if (!this.FileSystem.Directory.Exists(configDirectory))
				return true;

			this.Session.SendActionStart(1000, ActionName, "Removing data, logs, and config directory",
				"Removing directories: [1]");

			var yamlConfiguration = ElasticsearchYamlConfiguration.FromFolder(configDirectory);
			var dataDirectory = yamlConfiguration?.Settings?.DataPath ?? string.Empty;
			var logsDirectory = yamlConfiguration?.Settings?.LogsPath ?? string.Empty;

			if (this.FileSystem.Directory.Exists(dataDirectory))
				this.DeleteDirectory(dataDirectory);

			if (this.FileSystem.Directory.Exists(logsDirectory))
			{
				DumpElasticsearchLogOnRollback(logsDirectory);
				this.DeleteDirectory(logsDirectory);
			}

			if (this.FileSystem.Directory.Exists(configDirectory))
				this.DeleteDirectory(configDirectory);

			this.Session.SendProgress(1000, "Data, logs, and config directories removed");

			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;

			if (!this.FileSystem.Directory.Exists(installDirectory))
				return true;

			var directories = this.FileSystem.Directory.GetDirectories(installDirectory).ToList();

			this.Session.SendActionStart((directories.Count * 1000) + 1000, ActionName, "Removing Elasticsearch installation directory",
				"Removing directories: [1]");
			foreach (var directory in directories)
			{
				this.Session.Log($"Attemping to delete {directory}");
				this.FileSystem.Directory.Delete(directory, true);
				this.Session.SendProgress(1000, $"{directory} removed");
			}

			if (Empty(installDirectory))
				this.FileSystem.Directory.Delete(installDirectory, true);

			if (Empty(LocationsModel.DefaultCompanyDirectory))
				this.FileSystem.Directory.Delete(LocationsModel.DefaultCompanyDirectory, true);

			this.Session.SendProgress(1000, "Elasticsearch installation directory removed");
			return true;
		}

		private void DumpElasticsearchLogOnRollback(string logsDirectory)
		{
			if (!this.RollBack) return;
			var clusterName = this.InstallationModel.ConfigurationModel.ClusterName;
			var logFile = Path.Combine(logsDirectory, clusterName) + ".log";
			if (this.FileSystem.File.Exists(logFile))
			{
				this.Session.Log($"Elasticsearch log file found: {logFile}");
				var log = this.FileSystem.File.ReadAllText(logFile);
				this.Session.Log(log);
			}
			else
				this.Session.Log($"Elasticsearch log file not found: {logFile}");
		}

		private void DeleteDirectory(string directory)
		{
			this.Session.Log($"Attemping to delete {directory}");
			this.FileSystem.Directory.Delete(directory, true);
			this.Session.SendProgress(1000, $"{directory} removed");
		}

		private bool Empty(string path) => this.FileSystem.Directory.Exists(path) && !this.FileSystem.Directory.EnumerateFileSystemEntries(path).Any();
	}
}