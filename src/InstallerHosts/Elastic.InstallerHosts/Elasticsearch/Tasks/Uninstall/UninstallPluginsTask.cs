using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks
{
	public class UninstallPluginsTask : ElasticsearchInstallationTaskBase
	{
		public UninstallPluginsTask(string[] args, ISession session) 
			: base(args, session) { }

		public UninstallPluginsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var installDirectory = this.InstallationModel.LocationsModel.InstallDir;
			var configDirectory = this.InstallationModel.LocationsModel.ConfigDirectory;
			var provider = this.InstallationModel.PluginsModel.PluginStateProvider;
			var environmentVariables = new Dictionary<string, string> { { ElasticsearchEnvironmentStateProvider.ConfDir, configDirectory } };
			var plugins = provider.InstalledPlugins(installDirectory, environmentVariables);

			if (plugins.Count == 0)
			{
				this.Session.Log("No existing plugins to remove");
				return true;
			}

			var ticksPerPlugin = new[] { 20, 1930, 50 };
			var totalTicks = plugins.Count * ticksPerPlugin.Sum();		
			var additionalArguments = new [] { "--purge" };
			
			this.Session.SendActionStart(totalTicks, ActionName, "Removing existing Elasticsearch plugins", "Elasticsearch plugin: [1]");
			foreach (var plugin in plugins)
			{
				this.Session.SendProgress(ticksPerPlugin[0], $"removing {plugin}");
				
				provider.Remove(ticksPerPlugin[1], installDirectory, configDirectory, plugin, additionalArguments, environmentVariables);
				this.Session.SendProgress(ticksPerPlugin[2], $"removed {plugin}");
			}
			return true;
		}
	}
}