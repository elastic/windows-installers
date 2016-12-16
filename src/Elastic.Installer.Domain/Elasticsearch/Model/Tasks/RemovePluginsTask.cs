using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class RemovePluginsTask : InstallationTask
	{
		public RemovePluginsTask(string[] args, ISession session) 
			: base(args, session) { }

		public RemovePluginsTask(InstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var provider = this.InstallationModel.PluginsModel.PluginStateProvider;

			var plugins = provider.InstalledPlugins(installDirectory, configDirectory);

			if (plugins.Count == 0)
			{
				this.Session.Log("No existing plugins to remove");
				return true;
			}

			var totalTicks = plugins.Count * 2000;

			this.Session.SendActionStart(totalTicks, ActionName, "Removing existing Elasticsearch plugins", "Elasticsearch plugin: [1]");
			foreach (var plugin in plugins)
			{
				this.Session.SendProgress(20, $"removing {plugin}");
				provider.Remove(installDirectory, configDirectory, plugin, this.Session, 1930);
				this.Session.SendProgress(50, $"removed {plugin}");
			}
			return true;
		}
	}
}