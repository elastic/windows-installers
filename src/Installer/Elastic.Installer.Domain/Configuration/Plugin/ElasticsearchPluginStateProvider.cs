using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Installer.Domain.Configuration.Wix.Session;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public class ElasticsearchPluginStateProvider : PluginStateProviderBase
	{
		private const string FileProtocol = "file:///";
		private static readonly Regex ElasticsearchDownloadProgress = new Regex(@"\s*(?<percent>\d+)\%\s*");

		public ElasticsearchPluginStateProvider(ISession session, IFileSystem fileSystem)
			: base("elasticsearch", session, fileSystem) { }

		public override DataReceivedEventHandler CreateInstallHandler(int perDownloadIncrement, string plugin)
		{
			return (sender, args) =>
			{
				var message = args.Data;

				if (string.IsNullOrEmpty(message)) return;

				var downloadProgress = ElasticsearchDownloadProgress.Match(message);
				if (downloadProgress.Success)
				{
					this.Session.SendProgress(perDownloadIncrement, $"downloading {plugin} ({downloadProgress.Groups["percent"].Value}%)");
				}
			
				this.Session.Log(message);
			};
		}

		public override void Install(int pluginTicks, string installDirectory, string configDirectory, string plugin, string[] additionalArguments = null,
			IDictionary<string, string> environmentVariables = null)
		{
			if (this.FileSystem.File.Exists(plugin) && !plugin.StartsWith(FileProtocol))
				plugin = $"{FileProtocol}{plugin}"; 

			base.Install(pluginTicks, installDirectory, configDirectory, plugin, additionalArguments, environmentVariables);
		}
	}
}