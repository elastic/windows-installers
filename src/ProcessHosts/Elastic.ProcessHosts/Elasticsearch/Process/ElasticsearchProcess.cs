using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Elasticsearch.Process
{
	public class ElasticsearchProcess : ProcessBase
	{
		private bool NoColor { get; set; }

		public ElasticsearchProcess(ManualResetEvent completedHandle, IEnumerable<string> args)
			: this(
				new ObservableProcess(),
				null,
				null,
				ElasticsearchEnvironmentConfiguration.Default,
				JavaConfiguration.Default,
				completedHandle,args)
		{}

		public ElasticsearchProcess(
			IObservableProcess process,
			IConsoleOutHandler consoleOutHandler,
			IFileSystem fileSystem,
			ElasticsearchEnvironmentConfiguration env,
			JavaConfiguration java,
			ManualResetEvent completedHandle,
			IEnumerable<string> args)
			: base(
				process,
				consoleOutHandler ?? new ElasticsearchConsoleOutHandler(process?.UserInteractive ?? false),
				fileSystem,
				completedHandle)
		{
			CheckForBadEnvironmentVariables(env);
			
			var homeDirectory = env.HomeDirectory?.TrimEnd('\\')
				?? throw new StartupException("No ES_HOME variable set and no home directory could be inferred from the executable location");
			var configDirectory = env.ConfigDirectory?.TrimEnd('\\')
				?? throw new StartupException("ES_CONFIG was not explicitly set nor could it be determined from ES_HOME or the current executable location");

			this.HomeDirectory = homeDirectory;
			this.ConfigDirectory = configDirectory;

			var parsedArguments = this.ParseArguments(args);

			var javaHome = java.JavaHomeCanonical;
			if (javaHome == null)
				throw new StartupException("JAVA_HOME is not set and no Java installation could be found in the windows registry!");

			this.ProcessExe = java.JavaExecutable;
			if (!this.FileSystem.File.Exists(this.ProcessExe))
				throw new StartupException($"Java executable not found, this could be because of a faulty JAVA_HOME variable: {this.ProcessExe}");

			this.Arguments = this.CreateObservableProcessArguments(parsedArguments);
		}

		protected void CheckForBadEnvironmentVariables(ElasticsearchEnvironmentConfiguration c)
		{
			var errors = new Dictionary<string, string>();
			var classPathError = "Don't modify the classpath with ES_CLASSPATH, Best is to add ";
			classPathError += "additional elements via the plugin mechanism, or if code must really be ";
			classPathError += "added to the main classpath, add jars to lib, unsupported";
			ErrorIfExists(c, errors, "ES_CLASSPATH", (k,v) => classPathError);

			const string t = "to %ES_JAVA_OPTS%";
			const string i = "in jvm.options or add";
			ErrorIfExists(c, errors, "ES_MIN_MEM", (k, v) => $"{k}={v}: set -Xms{v} {i} \"-Xms{v}\" {t}");
			ErrorIfExists(c, errors, "ES_MAX_MEM", (k, v) => $"{k}={v}: set -Xmx{v} {i} \"-Xmx{v}\" {t}");
			ErrorIfExists(c, errors, "ES_HEAP_SIZE", (k, v) => $"{k}={v}: set -Xms{v} and -Xmx{v} {i} \"-Xms{v} -Xmx{v}\" {t}");
			ErrorIfExists(c, errors, "ES_HEAP_NEWSIZE", (k, v) => $"{k}={v}: set -Xmn{v} {i} \"-Xmn{v}\" {t}");
			ErrorIfExists(c, errors, "ES_DIRECT_SIZE", (k,v) => $"{k}={v}: set -XX:MaxDirectMemorySize={v} {i} \"-XX:MaxDirectMemorySize={v}\" {t}");
			ErrorIfExists(c, errors, "ES_USE_IPV4", (k,v) => $"{k}={v}: set -Djava.net.preferIPv4Stack=true {i} \"-Djava.net.preferIPv4Stack=true\" {t}");
			ErrorIfExists(c, errors, "ES_GC_OPTS", (k, v) => $"{k}={v}: set %ES_GC_OPTS: = and % {i} \"{v}\" {t}");
			ErrorIfExists(c, errors, "ES_GC_LOG_FILE", (k,v) => $"{k}={v}: set -Xloggc:{v} {i} \"-Xloggc:{v}\" {t}");

			if (errors.Count == 0) return;
			var helpText = errors.Values.Aggregate(new StringBuilder(), (sb, v) => sb.AppendLine(v), sb => sb.ToString());
			throw new StartupException("The following deprecated environment variables are set and preventing elasticsearch from starting", helpText);
		}

		private static void ErrorIfExists(ElasticsearchEnvironmentConfiguration c, Dictionary<string, string> errors, string variable, Func<string, string, string> errorMessage)
		{
			var v = c.GetEnvironmentVariable(variable);
			if (string.IsNullOrWhiteSpace(v)) return;
			errors.Add(variable, errorMessage(variable, v));
		}
		
		

		protected override void HandleMessage(ConsoleOut c)
		{
			if (!ElasticsearchConsoleOutParser.TryParse(c,
				out string date, out string level, out string section, out string node, out string message, out bool started)) return;

			if (this.Started || string.IsNullOrWhiteSpace(message)) return;

			if (!started) return;
			this.BlockingSubject.OnNext(this.StartedHandle);
			this.Started = true;
			this.StartedHandle.Set();
		}

		protected sealed override IEnumerable<string> CreateObservableProcessArguments(IEnumerable<string> args)
		{
			var libFolder = Path.Combine(this.HomeDirectory, "lib");
			if (!FileSystem.Directory.Exists(libFolder))
				throw new StartupException($"Expected a 'lib' directory inside: {this.HomeDirectory}");

			var jars = new HashSet<string>(FileSystem.Directory.GetFiles(libFolder));

			var elasticsearchJar = jars.FirstOrDefault(f => Path.GetFileName(f).StartsWith("elasticsearch-"));
			if (elasticsearchJar == null)
				throw new StartupException($"No elasticsearch jar found in: {libFolder}");

			jars.ExceptWith(new [] { elasticsearchJar });

			var libs = jars.ToArray();

			var javaOpts = new LocalJvmOptionsConfiguration(Path.Combine(this.ConfigDirectory, "jvm.options"));
			var classPath = $"{elasticsearchJar};{string.Join(";", libs)}";

			var arguments = javaOpts.Options
				.Concat(new []
				{
					$"-Delasticsearch",
					$"-Des.path.home=\"{this.HomeDirectory}\"",
					$"-cp \"{classPath}\" org.elasticsearch.bootstrap.Elasticsearch",
					$"-Epath.conf=\"{this.ConfigDirectory}\""
				})
				.Concat(args)
				.ToList();

			return arguments;
		}

		protected sealed override IEnumerable<string> ParseArguments(IEnumerable<string> args)
		{
			if (args == null) return Enumerable.Empty<string>();
			var newArgs = new List<string>();
			var esFlag = false;
			foreach (var arg in args)
			{
				var a = arg;
				if (arg == "-E")
				{
					esFlag = true;
					continue;
				}
				if (arg.StartsWith("-E"))
				{
					esFlag = true;
					a = arg.Remove(0, 2);
				}
				switch (a)
				{
					case "--no-color":
						this.NoColor = true;
						break;
					default:
						if (esFlag && a.StartsWith("path.conf"))
							this.ConfigDirectory = ParseKeyValue(a);
						else if (esFlag && a.StartsWith("path.home"))
							this.HomeDirectory = ParseKeyValue(a);
						else
							newArgs.Add(esFlag ? $"-E{a}" : a);
						break;
				}
				esFlag = false;
			}
			return newArgs;
		}



		private static string ParseKeyValue(string arg)
		{
			var kv = arg.Split(new [] {'='}, 2, StringSplitOptions.RemoveEmptyEntries);
			return kv.Length != 2 ? null : kv[1];
		}

	}
}
