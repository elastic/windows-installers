using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Process.ObservableWrapper;
using Microsoft.SqlServer.Server;

namespace Elastic.Installer.Domain.Process
{
	public class ElasticsearchProcess : ProcessBase
	{
		private readonly string[] _libs;
		public string ElasticsearchJar { get; private set; }
		public string LibDirectory { get; private set; }
		
		public string JavaOptions { get; private set; }
		public int Port { get; private set; }
		public bool NoColor { get; private set; }

		public ElasticsearchProcess() : this(null) { }

		public ElasticsearchProcess(IEnumerable<string> args) : base(args)
		{
			this.HomeDirectory = (this.HomeDirectory
				?? Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.Machine)
				?? Directory.GetParent(".").FullName).TrimEnd('\\');

			this.ConfigDirectory = (this.ConfigDirectory
				?? Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.Machine)
				?? Path.Combine(this.HomeDirectory, "config")).TrimEnd('\\');

			this.LibDirectory = Path.Combine(this.HomeDirectory, "lib");
			this.JavaOptions = new LocalJvmOptionsConfiguration(Path.Combine(this.ConfigDirectory, "jvm.options")).ToString();

			var libs = new HashSet<string>(Directory.GetFiles(this.LibDirectory));
			this.ElasticsearchJar = libs.First(f => Path.GetFileName(f).StartsWith("elasticsearch-"));
			libs.ExceptWith(new [] { this.ElasticsearchJar });
			this._libs = libs.ToArray();

			var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Machine);
			if (javaHome == null)
				throw new Exception("JAVA_HOME is not set!");

			this.ProcessExe = Path.Combine(javaHome, @"bin\java.exe");

			if (!File.Exists(this.ProcessExe))
				throw new Exception("JAVA_HOME set but bin\\java.exe is not found using it as the root");
		}

		protected override List<string> GetArguments()
		{
			this.Stop();

			var classPath = $"{this.ElasticsearchJar};{string.Join(";", _libs)}";

			var arguments = JavaOptions.Split(' ')
				.Concat(new string[]
				{
					$"-Delasticsearch",
					$"-Des.path.home=\"{this.HomeDirectory}\"",
					$"-cp \"{classPath}\" org.elasticsearch.bootstrap.Elasticsearch",
					$"-E path.conf=\"{this.ConfigDirectory}\""
				})
				.Concat(this.AdditionalArguments)
				.ToList();

			return arguments;
		}

		protected override void HandleMessage(ConsoleOut message)
		{
			var s = new ElasticsearchMessage(this.Started, message.Data);
			if (this.Started || string.IsNullOrWhiteSpace(s.Message)) return;

			string version; int? pid; int port;
			if (s.TryParseNodeInfo(out version, out pid))
			{
			}
			else if (s.TryGetPortNumber(out port))
				this.Port = port;
			else if (s.TryGetStartedConfirmation())
			{
				this.BlockingSubject.OnNext(this.StartedHandle);
				this.Started = true;
				this.StartedHandle.Set();
			}
		}

		protected override List<string> ParseArguments(IEnumerable<string> args)
		{
			var newArgs = new List<string>();
			if (args == null)
				return newArgs;
			var esFlag = false;
			foreach (var arg in args)
			{
				if (arg == "-E")
				{
					esFlag = true;
					continue;
				}

				if (arg == "--no-color")
					this.NoColor = true;
				else if (arg.StartsWith("path.conf"))
					this.ConfigDirectory = ParseKeyValue(arg);
				else if (arg.StartsWith("path.home"))
					this.HomeDirectory = ParseKeyValue(arg);
				else
					newArgs.Add(esFlag ? $"-E {arg}" : arg);

				esFlag = false;
			}
			return newArgs;
		}

		private string ParseKeyValue(string arg)
		{
			var kv = arg.Split('=');
			if (kv.Length != 2)
				return null;
			return kv[1];
		}

		protected override void WriteError(string message) => ElasticsearchConsole.WriteLine(ConsoleColor.Red, message);

		protected override void WriteSuccess(string message) => ElasticsearchConsole.WriteLine(message);
	}
}
