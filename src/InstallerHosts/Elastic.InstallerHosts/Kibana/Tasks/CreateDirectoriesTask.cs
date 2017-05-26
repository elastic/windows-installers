using System.IO;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Kibana;

namespace Elastic.InstallerHosts.Kibana.Tasks
{
	public class CreateDirectoriesTask : KibanaInstallationTask
	{
		public CreateDirectoriesTask(string[] args, ISession session) : base(args, session) { }
		public CreateDirectoriesTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		private const int TotalTicks = 4000;

		protected override bool ExecuteTask()
		{
			this.Session.SendActionStart(TotalTicks, ActionName, "Creating directories", "Creating directories: [1]");

			CreateConfigDirectory();

			return true;
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

			//make sure we move files and folders in Program Files/Kibana/config to new config directory as long as they are different
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
					var to = Path.Combine(configDirectory, dir);
					//do not overwrite existing directories
					if (this.FileSystem.Directory.Exists(to)) continue;
					this.Session.Log($"moving directory: {dir} to {to}");
					this.FileSystem.Directory.Move(dir, to);
				}
				this.Session.Log($"removing original config dir: {installConfigDirectory}");
				this.FileSystem.Directory.Delete(installConfigDirectory, true);
			}

			var completedMessage = $"Created config directory {configDirectory}.";
			if (actionsTaken != 2)
				this.Session.SendProgress(2 - actionsTaken, completedMessage);

			this.Session.Log(completedMessage);
		}
	}
}