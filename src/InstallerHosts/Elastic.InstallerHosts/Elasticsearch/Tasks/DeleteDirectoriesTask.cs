using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using Microsoft.Deployment.WindowsInstaller;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class DeleteDirectoriesTask : ElasticsearchInstallationTaskBase
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
			if (this.Session.IsRollback && this.InstallationModel.NoticeModel.ExistingVersionInstalled)
			{
				this.Session.Log($"Skipping {nameof(DeleteDirectoriesTask)}: Already installed and rolling back.");
				RestoreConfigDirectory();
				RestorePluginsDirectory();
				return true;
			}

			if (this.InstallationModel.NoticeModel.ExistingVersionInstalled && !this.Session.IsUninstalling)
			{
				this.Session.Log($"Skipping {nameof(DeleteDirectoriesTask)}: Already installed and not currently uninstalling");
				return true;
			}
			this.Session.Log($"Executing {nameof(DeleteDirectoriesTask)}");

			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			if (!this.FileSystem.Directory.Exists(configDirectory))
			{
				this.Session.Log($"Config directory does not exist aborting {configDirectory}");
				return true;
			}

			this.Session.SendActionStart(1000, ActionName, "Removing data, logs, and config directory",
				"Removing directories: [1]");

			var yamlConfiguration = ElasticsearchYamlConfiguration.FromFolder(configDirectory, this.FileSystem);
			var dataDirectory = yamlConfiguration?.Settings?.DataPath ?? this.InstallationModel.LocationsModel.DataDirectory;
			var logsDirectory = yamlConfiguration?.Settings?.LogsPath ?? this.InstallationModel.LocationsModel.LogsDirectory;

			if (this.FileSystem.Directory.Exists(dataDirectory))
				this.DeleteDirectory(dataDirectory);
			else this.Session.Log($"Data Directory does not exist, skipping {dataDirectory}");

			if (this.FileSystem.Directory.Exists(logsDirectory))
			{
				DumpElasticsearchLogOnRollback(logsDirectory);
				this.DeleteDirectory(logsDirectory);
			}
			else this.Session.Log($"Logs Directory does not exist, skipping {logsDirectory}");

			if (this.FileSystem.Directory.Exists(configDirectory))
				this.DeleteDirectory(configDirectory);
			else this.Session.Log($"Config Directory does not exist, skipping {configDirectory}");

			this.Session.SendProgress(1000, "data, logs, and config directories removed");
			this.Session.Log("data, logs, and config directories removed");

			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;

			if (!this.FileSystem.Directory.Exists(installDirectory))
			{
				this.Session.Log($"Install directory does not exist. aborting {installDirectory}");
				return true;
			}

			var directories = this.FileSystem.Directory.GetDirectories(installDirectory).ToList();

			this.Session.SendActionStart((directories.Count * 1000) + 1000, ActionName, "Removing Elasticsearch installation directory",
				"Removing directories: [1]");
			foreach (var directory in directories)
			{
				this.Session.Log($"Attemping to delete {directory}");
				this.FileSystem.Directory.Delete(directory, true);
				this.Session.SendProgress(1000, $"{directory} removed");
			}

			if (IsDirectoryEmpty(installDirectory))
			{
				this.Session.Log($"{installDirectory} exists and is empty. deleting");
				this.FileSystem.Directory.Delete(installDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultProductInstallationDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultProductInstallationDirectory} exists and is empty. deleting");
				this.FileSystem.Directory.Delete(LocationsModel.DefaultProductInstallationDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultCompanyInstallationDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultCompanyInstallationDirectory} exists and is empty. deleting");
				this.FileSystem.Directory.Delete(LocationsModel.DefaultCompanyInstallationDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultProductDataDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultProductDataDirectory} exists and is empty. deleting");
				this.FileSystem.Directory.Delete(LocationsModel.DefaultProductDataDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultCompanyDataDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultCompanyDataDirectory} exists and is empty. deleting");
				this.FileSystem.Directory.Delete(LocationsModel.DefaultCompanyDataDirectory, true);
			}
			
			this.Session.SendProgress(1000, "Elasticsearch installation directory removed");
			
			return true;
		}

		private void RestoreConfigDirectory()
		{
			var tempconfigDirectory = this.FileSystem.Path.Combine(this.TempProductInstallationDirectory, "config");
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			if (!this.FileSystem.Directory.Exists(tempconfigDirectory)) return;
			
			this.Session.Log("Restoring config directory");
			this.FileSystem.Directory.Delete(configDirectory, true);
			this.CopyDirectory(tempconfigDirectory, configDirectory);
			this.FileSystem.Directory.Delete(tempconfigDirectory, true);
		}

		private void RestorePluginsDirectory()
		{
			var fs = this.FileSystem;
			var path = fs.Path;
			var pluginsTempDirectory = path.Combine(this.TempProductInstallationDirectory, "plugins");
			var pluginsDirectory = path.Combine(this.InstallationModel.LocationsModel.InstallDir, "plugins");

			if (!fs.Directory.Exists(pluginsTempDirectory)) return;
			
			this.Session.Log("Restoring plugins directory");

			// delete any plugins that might have been installed
			var installDir = fs.DirectoryInfo.FromDirectoryName(pluginsDirectory);
			foreach (var file in installDir.GetFiles())
				fs.File.Delete(file.FullName);
			foreach (var dir in installDir.GetDirectories())
				fs.Directory.Delete(dir.FullName, true);

			// restore old plugins
			var directory = fs.DirectoryInfo.FromDirectoryName(pluginsTempDirectory);
			foreach (var file in directory.GetFiles())
			{
				fs.File.Copy(file.FullName, path.Combine(directory.FullName, file.Name));
				fs.File.Delete(file.FullName);
			}
			foreach (var dir in directory.GetDirectories())
			{
				CopyDirectory(dir, fs.Directory.CreateDirectory(path.Combine(directory.FullName, dir.Name)));
				fs.Directory.Delete(dir.FullName, true);
			}

			this.FileSystem.Directory.Delete(pluginsTempDirectory, true);
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
		
		protected bool IsDirectoryEmpty(string path) =>
			this.FileSystem.Directory.Exists(path) && !this.FileSystem.Directory.EnumerateFileSystemEntries(path).Any();
	}
}