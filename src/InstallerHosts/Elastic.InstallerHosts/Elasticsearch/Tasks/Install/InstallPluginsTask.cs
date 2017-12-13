using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.InstallerHosts.Elasticsearch.Tasks.Install
{
	public class InstallPluginsTask : ElasticsearchInstallationTaskBase
	{
		private const string EsJavaOpts = "ES_JAVA_OPTS";

		public InstallPluginsTask(string[] args, ISession session) 
			: base(args, session) { }
		public InstallPluginsTask(ElasticsearchInstallationModel model, ISession session, IFileSystem fileSystem) 
			: base(model, session, fileSystem) { }

		protected override bool ExecuteTask()
		{
			var pluginsModel = this.InstallationModel.PluginsModel;
			var plugins = pluginsModel.Plugins.ToList();
			if (plugins.Count == 0)
			{
				this.Session.Log("No plugins selected to install");
				return true;
			}

			var locationsModel = this.InstallationModel.LocationsModel;
			var installDirectory = locationsModel.InstallDir;
			var configDirectory = locationsModel.ConfigDirectory;
			var provider = pluginsModel.PluginStateProvider;
			var ticksPerPlugin = new[] { 20, 1930, 50 };
			var totalTicks = plugins.Count * ticksPerPlugin.Sum();
			var environmentVariables = new Dictionary<string, string> { { ElasticsearchEnvironmentStateProvider.ConfDir, configDirectory } };			
			var httpProxy = pluginsModel.HttpProxyHost;
			var httpsProxy = pluginsModel.HttpsProxyHost;			
			var esJavaOpts = new List<string>(5);
			
			if (!string.IsNullOrEmpty(httpProxy))
			{
				esJavaOpts.Add($"-Dhttp.proxyHost=\"{httpProxy}\"");
				var httpProxyPort = pluginsModel.HttpProxyPort;
				if (httpProxyPort.HasValue)
					esJavaOpts.Add($"-Dhttp.proxyPort={httpProxyPort}");
			}
			
			if (!string.IsNullOrEmpty(httpsProxy))
			{
				esJavaOpts.Add($"-Dhttps.proxyHost=\"{httpsProxy}\"");
				var httpsProxyPort = pluginsModel.HttpsProxyPort;
				if (httpsProxyPort.HasValue)
					esJavaOpts.Add($"-Dhttps.proxyPort={httpsProxyPort}");
			}

			var pluginsStaging = this.InstallationModel.PluginsModel.PluginsStaging;
			if (!string.IsNullOrEmpty(pluginsStaging))
				esJavaOpts.Add($"-Des.plugins.staging={pluginsStaging}");

			if (esJavaOpts.Any())
				environmentVariables.Add(EsJavaOpts, string.Join(" ", esJavaOpts));
					
			this.Session.SendActionStart(totalTicks, ActionName, "Installing Elasticsearch plugins", "Elasticsearch plugin: [1]");
			foreach (var plugin in plugins)
			{
				this.Session.SendProgress(ticksPerPlugin[0], $"installing {plugin}");
				provider.Install(ticksPerPlugin[1], installDirectory, configDirectory, plugin, new [] {"--batch"}, environmentVariables);
				this.Session.SendProgress(ticksPerPlugin[2], $"installed {plugin}");
			}
			return true;
		}
	}
}
