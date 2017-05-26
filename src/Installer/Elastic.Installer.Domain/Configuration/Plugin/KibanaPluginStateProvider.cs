using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Elastic.Installer.Domain.Configuration.Wix.Session;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public class KibanaPluginStateProvider : PluginStateProviderBase
	{
		private static readonly Regex Transferring = new Regex(@"\s*Transferring\s\d+\sbytes(?<dots>\.*)\s*");
		private static readonly Regex TransferComplete = new Regex(@"\s*Transfer\scomplete\s*");
		private static readonly Regex RetrievingMetadata = new Regex(@"\s*Retrieving\smetadata\sfrom\splugin\sarchive\s*");
		private static readonly Regex ExtractingPlugin = new Regex(@"\s*Extracting\splugin\sarchive\s*");
		private static readonly Regex ExtractionComplete = new Regex(@"\s*Extraction\scomplete\s*");
		private static readonly Regex OptimizingBundles = new Regex(@"\s*Optimizing\sand\scaching\sbrowser\sbundles\.+\s*");
		private static readonly Regex InstallationComplete = new Regex(@"\s*Plugin\sinstallation\scomplete\s*");

		public KibanaPluginStateProvider(ISession session, IFileSystem fileSystem) : base("kibana", session, fileSystem) { }

		public override DataReceivedEventHandler CreateInstallHandler(int perDownloadIncrement, string plugin)
		{
			// Transferring 108045644 bytes....................
			// Transfer complete
			// Retrieving metadata from plugin archive
			// Extracting plugin archive
			// Extraction complete
			// Optimizing and caching browser bundles...
			// Plugin installation complete
			return (sender, args) =>
			{
				var process = (System.Diagnostics.Process)sender;
				var message = args.Data;

				if (string.IsNullOrEmpty(message)) return;

				if (Transferring.IsMatch(message))
				{
					Session.SendProgress(perDownloadIncrement * 14, $"downloaded {plugin}");
				}
				else if (TransferComplete.IsMatch(message))
				{
					Session.SendProgress(perDownloadIncrement * 14, $"downloaded {plugin}");
				}
				else if (RetrievingMetadata.IsMatch(message))
				{
					Session.SendProgress(perDownloadIncrement * 14, $"retrieving metadata for {plugin}");
				}
				else if (ExtractingPlugin.IsMatch(message))
				{
					Session.SendProgress(perDownloadIncrement * 14, $"extracting plugin archive for {plugin}");
				}
				else if (ExtractionComplete.IsMatch(message))
				{
					Session.SendProgress(perDownloadIncrement * 14, $"extraction complete for {plugin}");
				}
				else if (OptimizingBundles.IsMatch(message))
				{
					Session.SendProgress(perDownloadIncrement * 16, $"optimizing and caching bundles for {plugin}");
				}
				else if (InstallationComplete.IsMatch(message))
				{
					Session.SendProgress(perDownloadIncrement * 14, $"installation complete for {plugin}");
				}
				
				Session.Log(message);
				if (message.Contains("[y/N]"))
					process.StandardInput.WriteLine("y");
			};
		}
	}
}