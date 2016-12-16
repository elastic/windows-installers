using Elastic.Installer.Domain.Session;
using System.IO;
using System.IO.Abstractions;
using System.Security.AccessControl;
using static System.Security.AccessControl.InheritanceFlags;
using System.Security.Principal;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class CreateDirectoriesTask : InstallationTask
	{
		public CreateDirectoriesTask(string[] args, ISession session) : base(args, session) { }
		public CreateDirectoriesTask(InstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 4000;
		private FileSystemAccessRule _fileAccess;

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(TotalTicks, ActionName, "Creating directories", "Creating directories: [1]");

			CreateDataDirectory();

			CreateLogsDirectory();

			CreateConfigDirectory();

			return true;
		}

		private void CreateLogsDirectory()
		{
			var logsDirectory = this.InstallationModel.LocationsModel.LogsDirectory;
			if (!this.FileSystem.Directory.Exists(logsDirectory))
			{
				this.Session.SendProgress(1000, "creating logs directory " + logsDirectory);
				this.FileSystem.Directory.CreateDirectory(logsDirectory);
			}
			else
				this.Session.SendProgress(1000, "using existing logs directory " + logsDirectory);
		}

		private void CreateDataDirectory()
		{
			var dataDirectory = this.InstallationModel.LocationsModel.DataDirectory;
			if (!this.FileSystem.Directory.Exists(dataDirectory))
			{
				this.Session.SendProgress(1000, "creating data directory " + dataDirectory);
				this.FileSystem.Directory.CreateDirectory(dataDirectory);
			}
			else
				this.Session.SendProgress(1000, "using existing data directory " + dataDirectory);
		}

		private void CreateConfigDirectory()
		{
			//create new config directory if it does not already exist
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;

			var installConfigDirectory = Path.Combine(installDirectory, "config");

			int actionsTaken = 0;
			if (!this.FileSystem.Directory.Exists(configDirectory))
			{
				var message = $"default config directory not found. Creating directory {configDirectory}.";
				this.Session.SendProgress(1000, message);
				this.Session.Log(message);
				this.FileSystem.Directory.CreateDirectory(configDirectory);
				actionsTaken++;
			}

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
					var fileName = Path.GetFileName(file);
					var to = Path.Combine(configDirectory, fileName);
					//do not overwrite existing files
					if (this.FileSystem.File.Exists(to)) continue;

					this.Session.Log($"moving file: {file} to {to}");
					this.FileSystem.File.Move(file, to);
				}
				foreach (var dir in this.FileSystem.Directory.EnumerateDirectories(installConfigDirectory))
				{
					var dirName = Path.GetDirectoryName(dir);
					var to = Path.Combine(configDirectory, dir);
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
	}
}