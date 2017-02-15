using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Shared.Configuration
{
	public interface IPluginStateProvider
	{
		void Install(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments);
		void Remove(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments);
		IList<string> InstalledPlugins(string installDirectory, string configDirectory);
	}

	public class ElasticsearchPluginStateProvider : PluginStateProvider
	{
		private static readonly Regex ElasticsearchDownloadProgress = new Regex(@"\s*(?<percent>\d+)\%\s*");

		public ElasticsearchPluginStateProvider(ISession session, IFileSystem fileSystem) : base("elasticsearch", session, fileSystem)
		{
		}

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

	public class KibanaPluginStateProvider : PluginStateProvider
	{
		private static readonly Regex Transferring = new Regex(@"\s*Transferring\s\d+\sbytes(?<dots>\.*)\s*");
		private static readonly Regex TransferComplete = new Regex(@"\s*Transfer\scomplete\s*");
		private static readonly Regex RetrievingMetadata = new Regex(@"\s*Retrieving\smetadata\sfrom\splugin\sarchive\s*");
		private static readonly Regex ExtractingPlugin = new Regex(@"\s*Extracting\splugin\sarchive\s*");
		private static readonly Regex ExtractionComplete = new Regex(@"\s*Extraction\scomplete\s*");
		private static readonly Regex OptimizingBundles = new Regex(@"\s*Optimizing\sand\scaching\sbrowser\sbundles\.+\s*");
		private static readonly Regex InstallationComplete = new Regex(@"\s*Plugin\sinstallation\scomplete\s*");

		public KibanaPluginStateProvider(ISession session, IFileSystem fileSystem) : base("kibana", session, fileSystem)
		{
		}

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

	public abstract class PluginStateProvider : IPluginStateProvider
	{
		private readonly string _product;
		private readonly IFileSystem _fileSystem;

		protected readonly ISession Session;

		public static PluginStateProvider ElasticsearchDefault(ISession session) => new ElasticsearchPluginStateProvider(session, new FileSystem());

		public static PluginStateProvider KibanaDefault(ISession session) => new KibanaPluginStateProvider(session, new FileSystem());

		private static readonly char[] SplitListing = { '\r', '\n' };

		private string PluginScript(string installDirectory) => Path.Combine(installDirectory, "bin", $"{_product}-plugin.bat");

		protected PluginStateProvider(string product, ISession session, IFileSystem fileSystem)
		{
			_product = product;
			Session = session;
			_fileSystem = fileSystem;
		}

		public IList<string> InstalledPlugins(string installDirectory, string configDirectory)
		{
			var pluginsDirectory = Path.Combine(installDirectory, "plugins");
			if (!this._fileSystem.Directory.Exists(pluginsDirectory) || !this._fileSystem.Directory.EnumerateFileSystemEntries(pluginsDirectory).Any())
				return new List<string>();
			
			var command = "list";
			var pluginScript = PluginScript(installDirectory);
			var process = PluginProcess(configDirectory, pluginScript, command);
			var sb = new StringBuilder();
			DataReceivedEventHandler processOnOutputDataReceived = (sender, args) =>
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;
				sb.AppendLine(message);
			};
			var invokeResult = this.InvokeProcess(process, processOnOutputDataReceived); 
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;
			var data = sb.ToString();
			if (exitCode != 0 || errors)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut: {errorOut}");

			return data.Split(SplitListing, StringSplitOptions.RemoveEmptyEntries)
					.Select(p => p.Split('@').FirstOrDefault())
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.ToList();
		}

		public abstract DataReceivedEventHandler CreateInstallHandler(int perDownloadIncrement, string plugin);

		public void Install(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments)
		{
			var downloadingTicks = pluginTicks * 0.9;
			var perDownloadIncrement = (int)Math.Floor(downloadingTicks / 100);
			var installingTicks = pluginTicks - perDownloadIncrement * 100;

			var command = $"install {plugin} {string.Join(" ", additionalArguments)}";
			var pluginScript = this.PluginScript(installDirectory);
			var process = PluginProcess(configDirectory, pluginScript, command);
			var processOnOutputDataReceived = CreateInstallHandler(perDownloadIncrement, plugin);
			var invokeResult = this.InvokeProcess(process, processOnOutputDataReceived); 
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;
			Session.SendProgress(installingTicks, $"installing {plugin}");
			if (exitCode != 0 || errors)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut:{errorOut}");
		}

		public void Remove(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments)
		{
			var command = $"remove {plugin} {string.Join(" ", additionalArguments)}";
			var pluginScript = this.PluginScript(installDirectory);
			var process = PluginProcess(configDirectory, pluginScript, command);

			DataReceivedEventHandler processOnOutputDataReceived = (sender, args) =>
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;
				Session.Log(message);
			};
			var invokeResult = this.InvokeProcess(process, processOnOutputDataReceived); 
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;

			Session.SendProgress(pluginTicks, $"removing {plugin}");
			if (exitCode != 0 || errors)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut:{errorOut}");
		}

		private Tuple<int, bool, string> InvokeProcess(System.Diagnostics.Process process, DataReceivedEventHandler processOnOutputDataReceived)
		{
			process.OutputDataReceived += processOnOutputDataReceived;

			var errorsBuilder = new StringBuilder();
			var errors = false;
			process.ErrorDataReceived += (s, a) =>
			{
				if (string.IsNullOrEmpty(a.Data)) return;
				errorsBuilder.AppendLine(a.Data);
				errors = true;
				processOnOutputDataReceived(s, a);
			};

			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();

			var exitCode = process.ExitCode;

			process.OutputDataReceived -= processOnOutputDataReceived;
			process.ErrorDataReceived -= processOnOutputDataReceived;
			process.Close();
			return Tuple.Create(exitCode, errors, errorsBuilder.ToString());
		}

		private static System.Diagnostics.Process PluginProcess(string configDirectory, string pluginScript, string command)
		{
			var start = new ProcessStartInfo
			{
				FileName = pluginScript,
				Arguments = command,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				RedirectStandardInput = true,
				ErrorDialog = false,
				CreateNoWindow = true,
				UseShellExecute = false
			};
			start.EnvironmentVariables["CONF_DIR"] = configDirectory;
			return new System.Diagnostics.Process { StartInfo = start };
		}
	}

	public class NoopPluginStateProvider : IPluginStateProvider
	{
		public List<string> InstalledAfter { get; } = new List<string>();
		public string[] InstalledBefore { get; }

		public NoopPluginStateProvider() { }
		public NoopPluginStateProvider(params string [] installedBefore)
		{
			this.InstalledBefore = installedBefore;
		}

		public void Install(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments) =>
			this.InstalledAfter.Add(plugin);

		public IList<string> InstalledPlugins(string installDirectory, string configDirectory) => 
			this.InstalledBefore?.ToList() ?? new List<string>();

		public void Remove(int pluginTicks, string installDirectory, string configDirectory, string plugin, params string[] additionalArguments)
		{
		}
	}
}
