using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Elasticsearch.Model.Tasks
{
	public class InstallPluginsTask : InstallationTask
	{
		public InstallPluginsTask(string[] args, ISession session) 
			: base(args, session) { }
		public InstallPluginsTask(InstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var provider = this.InstallationModel.PluginsModel.PluginStateProvider;

			var plugins = this.InstallationModel.PluginsModel.Plugins.ToList();
			if (plugins.Count == 0)
			{
				this.Session.Log("No plugins to selected to install");
				return true;
			}

			var totalTicks = plugins.Count * 2000;

			this.Session.SendActionStart(totalTicks, ActionName, "Installing Elasticsearch plugins", "Elasticsearch plugin: [1]");
			foreach (var plugin in plugins)
			{
				this.Session.SendProgress(20, $"installing {plugin}");
				provider.Install(installDirectory, configDirectory, plugin, this.Session, 1930);
				this.Session.SendProgress(50, $"installed {plugin}");
			}
			return true;
		}
	}
}