using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Rollback
{
	/// <summary>
	/// Rollback directories on installation/upgrade
	/// </summary>
	public class RollbackDirectoriesTask : ElasticsearchInstallationTaskBase
	{
		public RollbackDirectoriesTask(string[] args, ISession session) : base(args, session) { }

		public RollbackDirectoriesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			RestoreConfigDirectory();
			RestoreXPackDirectory();
			RestorePluginsDirectory();

			// only delete config, logs and data if they were created by this installer operation
			var configDirectoryExisted = this.Session.Get<bool>("ConfigDirectoryExists");
			var logsDirectoryExisted = this.Session.Get<bool>("LogsDirectoryExists");
			var dataDirectoryExisted = this.Session.Get<bool>("DataDirectoryExists");
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			if (!this.FileSystem.Directory.Exists(configDirectory))
			{
				this.Session.Log($"Config directory does not exist. skipping {configDirectory}");
			}
			else
			{
				var yamlConfiguration = ElasticsearchYamlConfiguration.FromFolder(configDirectory, this.FileSystem);
				var dataDirectory = yamlConfiguration?.Settings?.DataPath ?? this.InstallationModel.LocationsModel.DataDirectory;
				var logsDirectory = yamlConfiguration?.Settings?.LogsPath ?? this.InstallationModel.LocationsModel.LogsDirectory;

				this.Session.SendActionStart(3000, ActionName, "Removing data, logs, and config directory, if created",
					"Removing directories: [1]");
				this.DeleteDirectoryIfExistsAndCreated(dataDirectory, !dataDirectoryExisted);
				this.DumpElasticsearchLogOnRollback(logsDirectory);
				this.DeleteDirectoryIfExistsAndCreated(logsDirectory, !logsDirectoryExisted);
				this.DeleteDirectoryIfExistsAndCreated(configDirectory, !configDirectoryExisted);
			}

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

		private void RestoreXPackDirectory()
		{
			var fs = this.FileSystem;
			var path = fs.Path;
			var tempBinXPackDirectory = path.Combine(this.TempProductInstallationDirectory, "bin", "x-pack");
			var binXPackDirectory = path.Combine(this.InstallationModel.LocationsModel.InstallDir, "bin", "x-pack");

			// always delete bin\paxk if installed
			this.Session.Log("Deleting bin\\x-pack directory");
			if (this.FileSystem.Directory.Exists(binXPackDirectory))
				this.FileSystem.Directory.Delete(binXPackDirectory, true);

			if (!fs.Directory.Exists(tempBinXPackDirectory)) return;

			this.Session.Log("Restoring existing bin\\x-pack directory");
			this.CopyDirectory(tempBinXPackDirectory, binXPackDirectory);
			this.FileSystem.Directory.Delete(tempBinXPackDirectory, true);
		}

		private void RestorePluginsDirectory()
		{
			var fs = this.FileSystem;
			var path = fs.Path;
			var pluginsTempDirectory = path.Combine(this.TempProductInstallationDirectory, "plugins");
			var pluginsDirectory = path.Combine(this.InstallationModel.LocationsModel.InstallDir, "plugins");

			// always delete any plugins that might have been installed
			this.Session.Log("Deleting plugins directory");

			if (fs.Directory.Exists(pluginsDirectory))
			{
				var installDir = fs.DirectoryInfo.FromDirectoryName(pluginsDirectory);
				foreach (var file in installDir.GetFiles())
					fs.File.Delete(file.FullName);
				foreach (var dir in installDir.GetDirectories())
					fs.Directory.Delete(dir.FullName, true);
			}

			if (!fs.Directory.Exists(pluginsTempDirectory)) return;		
			this.Session.Log("Restoring existing plugins directory");

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

		private void DeleteDirectoryIfExistsAndCreated(string directory, bool created)
		{
			if (!this.FileSystem.Directory.Exists(directory))
			{
				this.Session.SendProgress(1000, $"{directory} does not exist. skipping");
			}
			else if (!created)
			{
				this.Session.SendProgress(1000, $"{directory} already existed. skipping");
			}
			else
			{
				this.FileSystem.Directory.Delete(directory, true);
				this.Session.SendProgress(1000, $"{directory} removed");
			}
		}
		
		protected bool IsDirectoryEmpty(string path) =>
			this.FileSystem.Directory.Exists(path) && !this.FileSystem.Directory.EnumerateFileSystemEntries(path).Any();
	}
}
