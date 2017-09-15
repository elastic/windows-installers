using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class PreserveInstallTask : ElasticsearchInstallationTaskBase
	{
		public PreserveInstallTask(string[] args, ISession session) : base(args, session) {}

		public PreserveInstallTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		protected override bool ExecuteTask()
		{
			// move the current installed plugins, to restore in case of rollback.
			// Current plugins could be removed and reinstalled in event of rollback,
			// but that would mean redownloading again
			MoveCurrentPlugins();

			// copy the directory, to restore in case of rollback
			CopyCurrentConfigDirectory();

			return true;
		}

		private void CopyCurrentConfigDirectory()
		{
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var tempConfigDirectory = this.FileSystem.Path.Combine(this.TempProductInstallationDirectory, "config");
			//make sure if for some reason the tempConfigDirectory is there its empty before copying over the current state
			if (this.FileSystem.Directory.Exists(tempConfigDirectory))
				this.FileSystem.Directory.Delete(tempConfigDirectory, true);
			
			if (!this.FileSystem.Directory.Exists(configDirectory)) return;
			
			this.Session.Log($"Copying existing config directory from {configDirectory} to {tempConfigDirectory}");
			this.CopyDirectory(configDirectory, tempConfigDirectory);
		}

		private void MoveCurrentPlugins()
		{
			var fs = this.FileSystem;
			var path = fs.Path;
			var pluginsDirectory = path.Combine(this.InstallationModel.LocationsModel.InstallDir, "plugins");
			var tempPluginsDirectory = path.Combine(this.TempProductInstallationDirectory, "plugins");

			//make sure if for some reason the tempPluginsDirectory is there its empty before copying over the current state
			if (fs.Directory.Exists(tempPluginsDirectory))
				fs.Directory.Delete(tempPluginsDirectory, true);

			if (!fs.Directory.Exists(pluginsDirectory)) return;
			
			this.Session.Log($"Moving existing plugin directory from {pluginsDirectory} to {tempPluginsDirectory}");
			
			fs.Directory.CreateDirectory(tempPluginsDirectory);
			
			var source = fs.DirectoryInfo.FromDirectoryName(pluginsDirectory);
			var target = fs.DirectoryInfo.FromDirectoryName(tempPluginsDirectory);

			// copy and delete. move does not work across volumes.
			foreach (var file in source.GetFiles())
			{
				fs.File.Copy(file.FullName, path.Combine(target.FullName, file.Name));
				fs.File.Delete(file.FullName);
			}
			foreach (var dir in source.GetDirectories())
			{
				CopyDirectory(dir, fs.Directory.CreateDirectory(path.Combine(target.FullName, dir.Name)));
				fs.Directory.Delete(dir.FullName, true);
			}
		}
	}
}
