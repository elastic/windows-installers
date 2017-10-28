using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;

namespace Elastic.Installer.Domain.Configuration.Plugin
{
	public abstract class PluginStateProviderBase : IPluginStateProvider
	{
		private readonly string _product;
		private readonly IFileSystem _fileSystem;

		protected readonly ISession Session;

		public static PluginStateProviderBase ElasticsearchDefault(ISession session) =>
			new ElasticsearchPluginStateProvider(session, new FileSystem());

		public static PluginStateProviderBase KibanaDefault(ISession session) =>
			new KibanaPluginStateProvider(session, new FileSystem());

		private static readonly char[] SplitListing = { '\r', '\n' };

		private string PluginScript(string installDirectory) => Path.Combine(installDirectory, "bin", $"{_product}-plugin.bat");

		protected PluginStateProviderBase(string product, ISession session, IFileSystem fileSystem)
		{
			_product = product;
			Session = session;
			_fileSystem = fileSystem;
		}

		public IList<string> InstalledPlugins(string installDirectory, string configDirectory, IDictionary<string, string> environmentVariables = null)
		{
			var pluginsDirectory = Path.Combine(installDirectory, "plugins");
			if (!this._fileSystem.Directory.Exists(pluginsDirectory)
			    || !this._fileSystem.Directory.EnumerateFileSystemEntries(pluginsDirectory).Any())
				return new List<string>();
			
			var command = "list";
			var pluginScript = PluginScript(installDirectory);
			var process = PluginProcess(configDirectory, pluginScript, command, environmentVariables);
			var sb = new StringBuilder();

			void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;
				sb.AppendLine(message);
			}

			var invokeResult = InvokeProcess(process, ProcessOnOutputDataReceived);
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;
			var data = sb.ToString();
			if (exitCode != 0 || errors)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut: {errorOut}");

			var plugins = data.Split(SplitListing, StringSplitOptions.RemoveEmptyEntries)
					.Select(p => p.Split('@').FirstOrDefault())
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.ToList();
			
			Session.Log($"installed plugins: {Environment.NewLine}{string.Join(Environment.NewLine, plugins)}");

			return plugins;
		}

		public abstract DataReceivedEventHandler CreateInstallHandler(int perDownloadIncrement, string plugin);

		public void Install(int pluginTicks, string installDirectory, string configDirectory, 
			string plugin, string[] additionalArguments = null, IDictionary<string, string> environmentVariables = null)
		{
			var downloadingTicks = pluginTicks * 0.9;
			var perDownloadIncrement = (int)Math.Floor(downloadingTicks / 100);
			var installingTicks = pluginTicks - perDownloadIncrement * 100;

			var command = $"install {plugin} {string.Join(" ", additionalArguments ?? Enumerable.Empty<string>())}";
			var pluginScript = this.PluginScript(installDirectory);
			var process = PluginProcess(configDirectory, pluginScript, command, environmentVariables);
			var processOnOutputDataReceived = CreateInstallHandler(perDownloadIncrement, plugin);
			var invokeResult = InvokeProcess(process, processOnOutputDataReceived);
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;
			Session.SendProgress(installingTicks, $"installing {plugin}");
			if (exitCode != 0 || errors)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut:{errorOut}");
		}

		public void Remove(int pluginTicks, string installDirectory, string configDirectory, 
			string plugin, string[] additionalArguments = null, IDictionary<string, string> environmentVariables = null)
		{
			var command = $"remove {plugin} {string.Join(" ", additionalArguments ?? Enumerable.Empty<string>())}";
			var pluginScript = this.PluginScript(installDirectory);
			var process = PluginProcess(configDirectory, pluginScript, command, environmentVariables);

			void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;
				Session.Log(message);
			}

			var invokeResult = InvokeProcess(process, ProcessOnOutputDataReceived);
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;

			Session.SendProgress(pluginTicks, $"removing {plugin}");
			if (exitCode != 0 || errors)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut:{errorOut}");
		}

		private static Tuple<int, bool, string> InvokeProcess(Process process, DataReceivedEventHandler processOnOutputDataReceived)
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

		private static Process PluginProcess(string configDirectory, string pluginScript, 
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
				{
					using (var stream = await client.OpenReadTaskAsync(new Uri("https://www.google.com")))
						return stream != null;
				}
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
