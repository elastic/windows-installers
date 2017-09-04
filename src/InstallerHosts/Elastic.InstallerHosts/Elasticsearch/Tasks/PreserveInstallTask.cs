using System;
using System.IO.Abstractions;
using System.Reflection;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class PreserveInstallTask : ElasticsearchInstallationTask
	{
		public PreserveInstallTask(string[] args, ISession session) : base(args, session) {}

		public PreserveInstallTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) {}

		protected override bool ExecuteTask()
		{
			if (this.FileSystem.Directory.Exists(this.TempDirectory))
				this.FileSystem.Directory.Delete(this.TempDirectory, true);

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
			if (this.FileSystem.Directory.Exists(configDirectory))
			{
				var tempConfigDirectory = this.FileSystem.Path.Combine(this.TempDirectory, "config");
				this.Session.Log($"Copying existing config directory from {configDirectory} to {tempConfigDirectory}");
				this.CopyDirectory(configDirectory, tempConfigDirectory);
			}
		}

		private void MoveCurrentPlugins()
		{
			var path = this.FileSystem.Path;
			var pluginsDirectory = path.Combine(this.InstallationModel.LocationsModel.InstallDir, "plugins");
			var tempPluginsDirectory = path.Combine(this.TempDirectory, "plugins");

			if (this.FileSystem.Directory.Exists(pluginsDirectory))
			{
				this.Session.Log($"Moving existing plugin directory from {pluginsDirectory} to {tempPluginsDirectory}");
				this.FileSystem.Directory.CreateDirectory(tempPluginsDirectory);
				var source = this.FileSystem.DirectoryInfo.FromDirectoryName(pluginsDirectory);

				foreach (var file in source.GetFiles())
					file.MoveTo(path.Combine(tempPluginsDirectory, file.Name));
				foreach (var dir in source.GetDirectories())
					dir.MoveTo(path.Combine(tempPluginsDirectory, dir.Name));
			}
		}
	}
}