using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.Extensions;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Elasticsearch.Process
{
	public interface IElasticsearchTool
	{
		bool Start(string[] arguments, out string output);
	}

	//TODO move this to Proc once we release that.
	//ObservableProcess as included in this solution is not very composable and reusable
	public class ElasticsearchTool : IElasticsearchTool
	{
		public string JavaClass { get; }

		public ElasticsearchTool(string javaClass, JavaConfiguration java, ElasticsearchEnvironmentConfiguration env, IFileSystem fileSystem)
		{
			this.JavaClass = javaClass;
			var homeDirectory = env.HomeDirectory?.TrimEnd('\\')
			                    ?? throw new StartupException(
				                    $"No {ElasticsearchEnvironmentStateProvider.EsHome} variable set and no home directory could be inferred from the executable location");

			this.WorkingDirectory = homeDirectory;

			var javaHome = java.JavaHomeCanonical;
			if (javaHome == null)
				throw new StartupException("JAVA_HOME is not set and no Java installation could be found in the windows registry!");

			this.ProcessExe = java.JavaExecutable;
			if (!fileSystem.File.Exists(this.ProcessExe))
				throw new StartupException($"Java executable not found, this could be because of a faulty JAVA_HOME variable: {this.ProcessExe}");

			var libFolder = Path.Combine(homeDirectory, "lib");
			if (!fileSystem.Directory.Exists(libFolder)) throw new StartupException($"Expected a 'lib' directory inside: {homeDirectory}");
			var classPath = $"{Path.Combine(libFolder, "*")}";

			this.ProcessVariables = new Dictionary<string, string>
			{
				{ "ES_TMPDIR", env.PrivateTempDirectory },
				{ "HOSTNAME", Environment.MachineName }
			};

			this.FileSystem = fileSystem;
			this.Arguments = new[]
			{
				$"-cp \"{classPath}\" {javaClass}"
			};
		}

		private IFileSystem FileSystem { get; }
		
		private Dictionary<string, string> ProcessVariables { get; }

		private string[] Arguments { get; }

		private string ProcessExe { get; }

		private string WorkingDirectory { get; }

		public bool Start(string[] arguments, out string output) => this.Start(arguments, TimeSpan.FromMinutes(1), out output);

		public bool Start(string[] arguments, TimeSpan timeout, out string output)
		{
			output = string.Empty;
			arguments = this.Arguments.Concat(arguments ?? new string[0] { }).ToArray() ;
			var args = string.Join(" ", arguments);
			
			using (var p = new System.Diagnostics.Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = this.ProcessExe,
					WorkingDirectory = this.WorkingDirectory,
					Arguments = args,
					CreateNoWindow = true,
					ErrorDialog = false,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = false,
				}
			})
			{
				foreach (var kv in this.ProcessVariables) p.StartInfo.EnvironmentVariables[kv.Key] = kv.Value;
				var sb = new StringBuilder();
				p.OutputDataReceived += (o, d) =>
				{
					if (!string.IsNullOrEmpty(d.Data)) sb.AppendLine(d.Data);
				};
				p.ErrorDataReceived += (o, d) => 
				{
					if (!string.IsNullOrEmpty(d.Data)) sb.AppendLine(d.Data);
				};

				try
				{
					if (!p.Start()) return false;
				}
				catch (Exception e)
				{
					sb.AppendLine($"Exception thrown starting process {this.ProcessExe} with arguments {args}: {e}");
					output = sb.ToString();			
					return false;
				}
				
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				
				if (!p.WaitForExit((int) timeout.TotalMilliseconds)) return false;
				p.WaitForExit();
				output = sb.ToString();
				return p.ExitCode == 0;
			}
		}

		public static ElasticsearchTool JvmOptionsParser =>
			new ElasticsearchTool(
				"org.elasticsearch.tools.launchers.JvmOptionsParser", 
				JavaConfiguration.Default,
				ElasticsearchEnvironmentConfiguration.Default, 
				new FileSystem());

		public static ElasticsearchTool JavaVersionChecker =>
			new ElasticsearchTool(
				"org.elasticsearch.tools.java_version_checker.JavaVersionChecker",
				JavaConfiguration.Default,
				ElasticsearchEnvironmentConfiguration.Default,
				new FileSystem());
	}
}