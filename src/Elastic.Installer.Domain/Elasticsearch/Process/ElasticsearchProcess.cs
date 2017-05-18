using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Process;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Elasticsearch.Process
{
	public class ElasticsearchProcess : ProcessBase
	{
		private bool NoColor { get; set; }

		public ElasticsearchProcess(IEnumerable<string> args)
			: this(null, null, null, ElasticsearchEnvironmentConfiguration.Default, JavaConfiguration.Default, args)
		{}

		public ElasticsearchProcess(
			IObservableProcess process,
			IConsoleOutHandler consoleOutHandler,
			IFileSystem fileSystem,
			ElasticsearchEnvironmentConfiguration env,
			JavaConfiguration java,
			IEnumerable<string> args)
			: base(process, consoleOutHandler ?? new ElasticsearchConsoleOutHandler(process?.UserInteractive ?? false), fileSystem)
		{
			var homeDirectory = (env.HomeDirectory ?? FileSystem.Directory.GetParent(".").FullName).TrimEnd('\\');
			var configDirectory = (env.ConfigDirectory ?? Path.Combine(homeDirectory, "config")).TrimEnd('\\');

			this.HomeDirectory = homeDirectory;
			this.ConfigDirectory = configDirectory;

			var parsedArguments = this.ParseArguments(args);

			var javaHome = java.JavaHomeCanonical;
			if (javaHome == null)
				throw new Exception("JAVA_HOME is not set and no Java installation could be found in the windows registry!");

			this.ProcessExe = java.JavaExecutable;
			if (!fileSystem.File.Exists(this.ProcessExe))
				throw new Exception($"Java executable not found, this could be because of a faulty JAVA_HOME variable: {this.ProcessExe}");

			this.Arguments = this.CreateObservableProcessArguments(parsedArguments);
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
				throw new Exception($"Expected a 'lib' directory inside: {this.HomeDirectory}");

			var jars = new HashSet<string>(FileSystem.Directory.GetFiles(libFolder));

			var elasticsearchJar = jars.FirstOrDefault(f => Path.GetFileName(f).StartsWith("elasticsearch-"));
			if (elasticsearchJar == null)
				throw new Exception($"No elasticsearch jar found in: {libFolder}");

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
					$"-E path.conf=\"{this.ConfigDirectory}\""
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
				switch (arg)
				{
					case "-E": //to support both -Eoption and -E option
						esFlag = true;
						continue;
					case "--no-color":
						this.NoColor = true;
						break;
					default:
						if (arg.StartsWith("path.conf"))
							this.ConfigDirectory = ParseKeyValue(arg);
						else if (arg.StartsWith("path.home"))
							this.HomeDirectory = ParseKeyValue(arg);
						else
							newArgs.Add(esFlag ? $"-E {arg}" : arg);
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
