using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Uninstall
{
	/// <summary>
	/// Ensures we remove the Elasticsearch installation directory completely.
	/// We can't rely on the MSI RemoveFiles step to remove directories/files that were registered 
	/// on a full uninstall because new files may have been introduced later on (e.g. plugins).
	/// </summary>
	public class UninstallDirectoriesTask : ElasticsearchInstallationTaskBase
	{
		public UninstallDirectoriesTask(string[] args, ISession session) 
			: base(args, session) { }

		public UninstallDirectoriesTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem)
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			this.Session.Log($"Executing {nameof(UninstallDirectoriesTask)}");

			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var directories = new List<string>();
			var fs = this.FileSystem;

			if (fs.Directory.Exists(installDirectory))
			{
				// if config, logs or data directories are rooted in the install directory,
				// exclude them from deletion.
				var installSubDirectories = fs.Directory.GetDirectories(installDirectory)
					.Where(d => d != this.InstallationModel.LocationsModel.ConfigDirectory &&
					            d != this.InstallationModel.LocationsModel.LogsDirectory &&
					            d != this.InstallationModel.LocationsModel.DataDirectory);

				directories.AddRange(installSubDirectories);
			}

			this.Session.SendActionStart((directories.Count * 1000) + 1000, ActionName, "Removing Elasticsearch installation directory",
				"Removing directories: [1]");

			foreach (var directory in directories)
			{
				this.Session.Log($"Attemping to delete {directory}");
				fs.Directory.Delete(directory, true);
				this.Session.SendProgress(1000, $"{directory} removed");
			}

			if (IsDirectoryEmpty(installDirectory))
			{
				this.Session.Log($"{installDirectory} exists and is empty. deleting");
				fs.Directory.Delete(installDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultProductInstallationDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultProductInstallationDirectory} exists and is empty. deleting");
				fs.Directory.Delete(LocationsModel.DefaultProductInstallationDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultCompanyInstallationDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultCompanyInstallationDirectory} exists and is empty. deleting");
				fs.Directory.Delete(LocationsModel.DefaultCompanyInstallationDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultProductDataDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultProductDataDirectory} exists and is empty. deleting");
				fs.Directory.Delete(LocationsModel.DefaultProductDataDirectory, true);
			}

			if (IsDirectoryEmpty(LocationsModel.DefaultCompanyDataDirectory))
			{
				this.Session.Log($"{LocationsModel.DefaultCompanyDataDirectory} exists and is empty. deleting");
				fs.Directory.Delete(LocationsModel.DefaultCompanyDataDirectory, true);
			}

			this.Session.SendProgress(1000, "Elasticsearch installation directory removed");

			return true;
		}

		protected bool IsDirectoryEmpty(string path) =>
			this.FileSystem.Directory.Exists(path) && !this.FileSystem.Directory.EnumerateFileSystemEntries(path).Any();
	}
}