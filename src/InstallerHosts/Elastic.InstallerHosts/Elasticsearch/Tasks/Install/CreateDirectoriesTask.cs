using System.IO.Abstractions;
using System.Security.AccessControl;
using System.Security.Principal;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class CreateDirectoriesTask : ElasticsearchInstallationTaskBase
	{
		public CreateDirectoriesTask(string[] args, ISession session) : base(args, session) { }
		public CreateDirectoriesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 5000;

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(TotalTicks, ActionName, "Creating directories", "Creating directories: [1]");
  
			var rule = new FileSystemAccessRule(
				new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null),
				FileSystemRights.Write,
				InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
				PropagationFlags.None,
				AccessControlType.Allow);

			CreateDataDirectory(rule);

			CreateLogsDirectory(rule);

			CreateConfigDirectory(rule);

			CreatePluginsDirectory();

			return true;
		}

		private void CreatePluginsDirectory()
		{
			var pluginsDirectoryName = "plugins";
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;			
			var pluginsDirectory = this.FileSystem.Path.Combine(installDirectory, pluginsDirectoryName);

			if (!this.FileSystem.Directory.Exists(pluginsDirectory))
			{
				this.Session.SendProgress(1000, $"creating plugins directory {pluginsDirectory}");
				this.FileSystem.Directory.CreateDirectory(pluginsDirectory);
			}
			else
				this.Session.SendProgress(1000, $"using existing plugins directory {pluginsDirectory}");
		}

		private void CreateLogsDirectory(FileSystemAccessRule rule)
		{
			var logsDirectory = this.InstallationModel.LocationsModel.LogsDirectory;
			if (!this.FileSystem.Directory.Exists(logsDirectory))
			{
				this.Session.SendProgress(1000, "creating logs directory " + logsDirectory);
				this.FileSystem.Directory.CreateDirectory(logsDirectory);
			}
			else
				this.Session.SendProgress(1000, "using existing logs directory " + logsDirectory);

			SetAccessControl(logsDirectory, rule);
		}

		private void CreateDataDirectory(FileSystemAccessRule rule)
		{
			var dataDirectory = this.InstallationModel.LocationsModel.DataDirectory;
			if (!this.FileSystem.Directory.Exists(dataDirectory))
			{
				this.Session.SendProgress(1000, "creating data directory " + dataDirectory);
				this.FileSystem.Directory.CreateDirectory(dataDirectory);
			}
			else
				this.Session.SendProgress(1000, "using existing data directory " + dataDirectory);

			SetAccessControl(dataDirectory, rule);
		}

		private void CreateConfigDirectory(FileSystemAccessRule rule)
		{
			//create new config directory if it does not already exist
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var configDirectoryName = "config";
			var installConfigDirectory = this.FileSystem.Path.Combine(installDirectory, configDirectoryName);

			int actionsTaken = 0;
			if (!this.FileSystem.Directory.Exists(configDirectory))
			{
				var message = $"default config directory not found. Creating directory {configDirectory}.";
				this.Session.SendProgress(1000, message);
				this.Session.Log(message);
				this.FileSystem.Directory.CreateDirectory(configDirectory);
				actionsTaken++;
			}

			SetAccessControl(configDirectory, rule);

			//make sure we move files and folders in ES_HOME/config to new ES_CONFIG as long as they are different
			if (this.FileSystem.Directory.Exists(installConfigDirectory) && !this.SamePathAs(installConfigDirectory, configDirectory))
			{
				var message = $"syncing install config directory {installConfigDirectory} with {configDirectory}.";
				this.Session.SendProgress(1000, message);
				this.Session.Log(message);

				if (!installConfigDirectory.EndsWith(@"\") || !installConfigDirectory.EndsWith("/"))
					installConfigDirectory += @"\";

				foreach (var file in this.FileSystem.Directory.EnumerateFiles(installConfigDirectory))
				{
					var fileName = this.FileSystem.Path.GetFileName(file);
					var to = this.FileSystem.Path.Combine(configDirectory, fileName);
					//do not overwrite existing files
					if (this.FileSystem.File.Exists(to)) continue;

					this.Session.Log($"moving file: {file} to {to}");
					this.FileSystem.File.Move(file, to);
				}
				foreach (var dir in this.FileSystem.Directory.EnumerateDirectories(installConfigDirectory))
				{
					var directoryName = this.FileSystem.DirectoryInfo.FromDirectoryName(dir).Name;
					var to = this.FileSystem.Path.Combine(configDirectory, directoryName);
					//do not overwrite existing directories
					if (this.FileSystem.Directory.Exists(to)) continue;

					this.Session.Log($"moving directory: {dir} to {to}");
					this.FileSystem.Directory.Move(dir, to);
				}
				this.Session.Log($"removing ES_HOME config dir: {installConfigDirectory}");
				this.FileSystem.Directory.Delete(installConfigDirectory, true);
			}

			var completedMessage = $"Created config directory {configDirectory}.";
			if (actionsTaken != 2)
				this.Session.SendProgress(2 - actionsTaken, completedMessage);

			this.Session.Log(completedMessage);
		}

		private void SetAccessControl(string directory, FileSystemAccessRule rule)
		{
			var directoryInfo = FileSystem.DirectoryInfo.FromDirectoryName(directory);
			var directorySecurity = directoryInfo.GetAccessControl();
			directorySecurity.AddAccessRule(rule);
			directoryInfo.SetAccessControl(directorySecurity);
		}
	}
}
