using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Kibana;

namespace Elastic.InstallerHosts.Kibana.Tasks
{
	public class InstallPluginsTask : KibanaInstallationTask
	{
		public InstallPluginsTask(string[] args, ISession session) 
			: base(args, session) { }
		public InstallPluginsTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var plugins = this.InstallationModel.PluginsModel.Plugins.ToList();

			if (plugins.Count == 0)
			{
				this.Session.Log("No plugins selected to install");
				return true;
			}

			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var provider = this.InstallationModel.PluginsModel.PluginStateProvider;
			var ticksPerPlugin = new[] { 20, 1930, 50 };
			var totalTicks = plugins.Count * ticksPerPlugin.Sum();
			var configFile = Path.Combine(configDirectory, "kibana.yml");

			this.Session.SendActionStart(totalTicks, ActionName, "Installing Kibana plugins", "Kibana plugin: [1]");
			foreach (var plugin in plugins)
			{
				this.Session.SendProgress(ticksPerPlugin[0], $"installing {plugin}");
				var additionalArguments = new [] { "--config", configFile };
				provider.Install(ticksPerPlugin[1], installDirectory, configDirectory, plugin, additionalArguments);
				this.Session.SendProgress(ticksPerPlugin[2], $"installed {plugin}");
			}
			return true;
		}
	}
}