using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Installer.Domain.Configuration.Wix.Session;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public class ElasticsearchPluginStateProvider : PluginStateProviderBase
	{
		private static readonly Regex ElasticsearchDownloadProgress = new Regex(@"\s*(?<percent>\d+)\%\s*");

		public ElasticsearchPluginStateProvider(ISession session, IFileSystem fileSystem)
			: base("elasticsearch", session, fileSystem) { }

		public override DataReceivedEventHandler CreateInstallHandler(int perDownloadIncrement, string plugin)
		{
			return (sender, args) =>
			{
				var process = (System.Diagnostics.Process)sender;
				var message = args.Data;

				if (string.IsNullOrEmpty(message)) return;

				var downloadProgress = ElasticsearchDownloadProgress.Match(message);
				if (downloadProgress.Success)
				{
					this.Session.SendProgress(perDownloadIncrement, $"downloading {plugin} ({downloadProgress.Groups["percent"].Value}%)");
				}
			
				this.Session.Log(message);
				if (message.Contains("[y/N]"))
					process.StandardInput.WriteLine("y");
			};
		}
	}
}