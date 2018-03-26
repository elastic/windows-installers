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
	//TODO move this to Proc once we release that.
	//ObservableProcess as included in this solution is not very composable and reusable
	public class ElasticsearchTool
	{
		public string JavaClass { get; }

		public ElasticsearchTool(string javaClass, JavaConfiguration java, ElasticsearchEnvironmentConfiguration env, IFileSystem fileSystem)
		{
			this.JavaClass = javaClass;
			var homeDirectory = env.HomeDirectory?.TrimEnd('\\')
			                    ?? throw new StartupException(
				                    $"No {ElasticsearchEnvironmentStateProvider.EsHome} variable set and no home directory could be inferred from the executable location");

			var javaHome = java.JavaHomeCanonical;
			if (javaHome == null)
				throw new StartupException("JAVA_HOME is not set and no Java installation could be found in the windows registry!");

			this.ProcessExe = java.JavaExecutable;
			if (!fileSystem.File.Exists(this.ProcessExe))
				throw new StartupException($"Java executable not found, this could be because of a faulty JAVA_HOME variable: {this.ProcessExe}");

			var libFolder = Path.Combine(homeDirectory, "lib");
			if (!fileSystem.Directory.Exists(libFolder)) throw new StartupException($"Expected a 'lib' directory inside: {homeDirectory}");
			var classPath = $"{Path.Combine(libFolder, "*")}";

			this.ProcessVariables = new Dictionary<string, string>();
			this.ProcessVariables["ES_TMPDIR"] = env.PrivateTempDirectory;
			this.ProcessVariables["HOSTNAME"] = Environment.MachineName;
			this.OutputFile = fileSystem.Path.GetTempFileName();
			this.FileSystem = fileSystem;

			this.Arguments = new[]
			{
				$"-cp \"{classPath}\" {javaClass}"
			};
		}

		private IFileSystem FileSystem { get; }
		
		private string OutputFile { get; }

		private Dictionary<string, string> ProcessVariables { get; }

		private string[] Arguments { get; }

		private string ProcessExe { get; }

		public bool Start(string[] arguments, out string stdOut) => this.Start(arguments, TimeSpan.FromMinutes(1), out stdOut);

		public bool Start(string[] arguments, TimeSpan timeout, out string stdOut)
		{
			stdOut = string.Empty;
			arguments = this.Arguments.Concat(arguments ?? new string[0] { }).ToArray() ;
			var args = string.Join(" ", arguments);
			
			using (var p = new System.Diagnostics.Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = this.ProcessExe,
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
				if (!p.Start()) return false;
				
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				

				if (!p.WaitForExit((int) timeout.TotalMilliseconds)) return false;
				p.WaitForExit();
				stdOut = sb.ToString();
				return p.ExitCode == 0;
			}
		}
	}
}