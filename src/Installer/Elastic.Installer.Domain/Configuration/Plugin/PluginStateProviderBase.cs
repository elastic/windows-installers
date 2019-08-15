using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Configuration.Wix.Session;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public abstract class PluginStateProviderBase : IPluginStateProvider
	{
		private readonly string _product;
		protected readonly IFileSystem FileSystem;

		protected readonly ISession Session;

		public static PluginStateProviderBase ElasticsearchDefault(ISession session) =>
			new ElasticsearchPluginStateProvider(session, new FileSystem());

		public static PluginStateProviderBase KibanaDefault(ISession session) =>
			new KibanaPluginStateProvider(session, new FileSystem());

		private static readonly char[] SplitListing = { '\r', '\n' };

		private string PluginScript(string installDirectory) => Path.Combine(installDirectory, "bin", $"{_product}-plugin.bat");

		protected PluginStateProviderBase(string product, ISession session, IFileSystem fileSystem)
		{
			this._product = product;
			this.Session = session;
			this.FileSystem = fileSystem;
		}

		public IList<string> InstalledPlugins(string installDirectory, IDictionary<string, string> environmentVariables = null)
		{
			var pluginsDirectory = Path.Combine(installDirectory, "plugins");
			var pluginScript = PluginScript(installDirectory);

			if (!this.FileSystem.File.Exists(pluginScript) ||
				!this.FileSystem.Directory.Exists(pluginsDirectory) || 
				!this.FileSystem.Directory.EnumerateFileSystemEntries(pluginsDirectory).Any())
				return new List<string>();
			
			var command = "list";
			var process = PluginProcess(pluginScript, command, environmentVariables);
			var sb = new StringBuilder();

			void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;
				sb.AppendLine(message);
			}

			var exitCode = InvokeProcess(process, ProcessOnOutputDataReceived);
			if (exitCode != 0)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode}");

			var plugins = sb.ToString().Split(SplitListing, StringSplitOptions.RemoveEmptyEntries)
					.Select(p => p.Split('@').FirstOrDefault())
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.ToList();
			
			return plugins;
		}

		public abstract DataReceivedEventHandler CreateInstallHandler(int perDownloadIncrement, string plugin);

		public virtual void Install(int pluginTicks, string installDirectory, string configDirectory, 
			string plugin, string[] additionalArguments = null, IDictionary<string, string> environmentVariables = null)
		{
			var downloadingTicks = pluginTicks * 0.9;
			var perDownloadIncrement = (int)Math.Floor(downloadingTicks / 100);
			var installingTicks = pluginTicks - perDownloadIncrement * 100;

			var command = $"install {plugin} {string.Join(" ", additionalArguments ?? Enumerable.Empty<string>())}";
			var pluginScript = this.PluginScript(installDirectory);
			var process = PluginProcess(pluginScript, command, environmentVariables);
			var processOnOutputDataReceived = CreateInstallHandler(perDownloadIncrement, plugin);
			var exitCode = InvokeProcess(process, processOnOutputDataReceived);

			Session.SendProgress(installingTicks, $"installing {plugin}");
			if (exitCode != 0)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode}");
		}

		public void Remove(int pluginTicks, string installDirectory, string configDirectory, 
			string plugin, string[] additionalArguments = null, IDictionary<string, string> environmentVariables = null)
		{
			var command = $"remove {plugin} {string.Join(" ", additionalArguments ?? Enumerable.Empty<string>())}";
			var pluginScript = this.PluginScript(installDirectory);
			var process = PluginProcess(pluginScript, command, environmentVariables);

			void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;
				Session.Log(message);
			}

			var exitCode = InvokeProcess(process, ProcessOnOutputDataReceived);

			Session.SendProgress(pluginTicks, $"removing {plugin}");
			if (exitCode != 0)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode}");
		}

		private static int InvokeProcess(Process process, DataReceivedEventHandler processOnOutputDataReceived)
		{
			process.OutputDataReceived += processOnOutputDataReceived;

			process.ErrorDataReceived += (s, a) =>
			{
				if (string.IsNullOrEmpty(a.Data)) return;
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

			return exitCode;
		}

		private static Process PluginProcess(string pluginScript, 
			string command, IEnumerable<KeyValuePair<string,string>> environmentVariables = null)
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

			if (environmentVariables != null)
			{
				foreach (var environmentVariable in environmentVariables)
					start.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;
			}
			
			return new Process { StartInfo = start };
		}

		private bool _hasInterNetConnection;
		public async Task<bool> HasInternetConnection()
		{
			_hasInterNetConnection = await CanReadArtifactsUrl();
			return _hasInterNetConnection;
		}

		private static async Task<bool> CanReadArtifactsUrl()
		{
			try
			{
				using (var client = new MyWebClient())
				using (var stream = await client.OpenReadTaskAsync(new Uri("https://www.google.com")))
					return stream != null;
			}
			catch
			{
				return false;
			}
		}

		private class MyWebClient : WebClient
		{
			protected override WebRequest GetWebRequest(Uri uri)
			{
				var w = base.GetWebRequest(uri);
				w.Timeout = 2000;
				return w;
			}
		}
	}
}
