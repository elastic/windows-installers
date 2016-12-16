using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration
{
	public interface IPluginStateProvider
	{
		void Install(string installDirectory, string configDirectory, string plugin, ISession session, int pluginTicks);
		void Remove(string installDirectory, string configDirectory, string plugin, ISession session, int pluginTicks);
		IList<string> InstalledPlugins(string installDirectory, string configDirectory);
	}

	public class PluginStateProvider : IPluginStateProvider
	{
		public static PluginStateProvider Default { get; } = new PluginStateProvider();
		private static readonly Regex DownloadProgress = new Regex(@"\s*(?<percent>\d+)\%\s*");
		private static readonly char[] SplitListing = { '\r', '\n' };

		private static string PluginScript(string installDirectory) => Path.Combine(installDirectory, "bin", "elasticsearch-plugin.bat");

		public IList<string> InstalledPlugins(string installDirectory, string configDirectory)
		{
			var pluginsDirectory = Path.Combine(installDirectory, "plugins");
			if (!Directory.Exists(pluginsDirectory) || !Directory.EnumerateFileSystemEntries(pluginsDirectory).Any())
				return new List<string>();
			
			var command = "list";
			var pluginScript = PluginScript(installDirectory);
			var process = this.PluginProcess(configDirectory, pluginScript, command);
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
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut:{errorOut}");

			return data.Split(SplitListing, StringSplitOptions.RemoveEmptyEntries)
					.Select(p => p.Split('@').FirstOrDefault())
					.Where(p => !string.IsNullOrWhiteSpace(p))
					.ToList();
		}

		public void Install(string installDirectory, string configDirectory, string plugin, ISession session, int pluginTicks)
		{
			var downloadingTicks = pluginTicks * 0.9;
			var perDownloadIncrement = (int)Math.Floor(downloadingTicks / 100);
			var installingTicks = pluginTicks - (perDownloadIncrement * 100);

			var command = $"install {plugin}";
			var pluginScript = PluginScript(installDirectory);
			var process = this.PluginProcess(configDirectory, pluginScript, command);

			DataReceivedEventHandler processOnOutputDataReceived = (sender, args) =>
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;

				var downloadProgress = DownloadProgress.Match(message);
				if (downloadProgress.Success)
					session.SendProgress(perDownloadIncrement, $"downloading {plugin} ({downloadProgress.Groups["percent"].Value}%)");

				session.Log(message);
				if (message.Contains("[y/N]"))
					process.StandardInput.WriteLine("y");
			};

			var invokeResult = this.InvokeProcess(process, processOnOutputDataReceived); 
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;
			session.SendProgress(installingTicks, $"installing {plugin}");
			if (exitCode != 0 || errors)
				throw new Exception($"Execution failed ({pluginScript} {command}). ExitCode: {exitCode} ErrorCheck: {errors} ErrorOut:{errorOut}");
		}

		public void Remove(string installDirectory, string configDirectory, string plugin, ISession session, int pluginTicks)
		{
			var command = $"remove {plugin}";
			var pluginScript = PluginScript(installDirectory);
			var process = this.PluginProcess(configDirectory, pluginScript, command);

			DataReceivedEventHandler processOnOutputDataReceived = (sender, args) =>
			{
				var message = args.Data;
				if (string.IsNullOrEmpty(message)) return;
				session.Log(message);
			};
			var invokeResult = this.InvokeProcess(process, processOnOutputDataReceived); 
			var exitCode = invokeResult.Item1;
			var errors = invokeResult.Item2;
			var errorOut = invokeResult.Item3;

			session.SendProgress(pluginTicks, $"removing {plugin}");
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

		private System.Diagnostics.Process PluginProcess(string configDirectory, string pluginScript, string command)
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

		public void Install(string installDirectory, string configDirectory, string plugin, ISession session, int pluginTicks)
		{
			this.InstalledAfter.Add(plugin);
		}

		public IList<string> InstalledPlugins(string installDirectory, string configDirectory) => this.InstalledBefore?.ToList() ?? new List<string>();

		public void Remove(string installDirectory, string configDirectory, string plugin, ISession session, int pluginTicks)
		{
		}
	}
}
