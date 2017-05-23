using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Kibana.Process
{
	public class KibanaProcess : ProcessBase
	{
		private string JsPath { get; }
		private string ConfigFile { get; }
		private string LogFile { get; }

		private string Host { get; set; }
		private int? Port { get; set; }

		public KibanaProcess(IEnumerable<string> args) : base(null, null, null)
		{
			this.HomeDirectory = (this.HomeDirectory
				?? Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_HOME_ENV_VAR, EnvironmentVariableTarget.Machine)
				?? Directory.GetParent(".").FullName).TrimEnd('\\');

			this.ConfigDirectory = (this.ConfigDirectory
				?? Environment.GetEnvironmentVariable(KibanaEnvironmentVariables.KIBANA_CONFIG_ENV_VAR, EnvironmentVariableTarget.Machine)
				?? Path.Combine(this.HomeDirectory, "config")).TrimEnd('\\');

			this.JsPath = Path.Combine(this.HomeDirectory, @"src\cli");
			this.ConfigFile = Path.Combine(this.ConfigDirectory, "kibana.yml");
			this.ProcessExe = Path.Combine(this.HomeDirectory, @"node\node.exe");

			var yamlConfig = KibanaYamlConfiguration.FromFolder(this.ConfigDirectory);
			this.LogFile = yamlConfig.Settings.LoggingDestination;
			var parsedArguments = this.ParseArguments(args);
			this.Arguments = this.CreateObservableProcessArguments(parsedArguments);
		}

		protected sealed override IEnumerable<string> CreateObservableProcessArguments(IEnumerable<string> args)
		{
			var arguments = args.Concat(new[]
			{
				"--no-warnings",
				$"\"{this.JsPath}\"",
				$"--config \"{this.ConfigFile}\""
			})
			.ToList();

			return arguments;
		}

		protected sealed override IEnumerable<string> ParseArguments(IEnumerable<string> args)
		{
			if (args == null) return Enumerable.Empty<string>();
			var newArgs = new List<string>();
			var nextArgIsConfigPath = false;
			foreach (var arg in args)
			{
				if (arg == "--config" || arg == "-c")
					nextArgIsConfigPath = true;
				if (nextArgIsConfigPath)
				{
					nextArgIsConfigPath = false;
					this.ConfigDirectory = arg;
				}
				else
					newArgs.Add(arg);

			}
			return newArgs;
		}

		public override void Start()
		{
			if (this.LogFile != "stdout")
			{
				var fileInfo = new FileInfo(LogFile);
				var seekTo = fileInfo.Exists ? fileInfo.Length : 0;
				var disposable = Observable.Interval(TimeSpan.FromSeconds(3))
					.TakeWhile(_ => !this.Started)
					.Subscribe(f =>
					{
						fileInfo.Refresh();
						if (!fileInfo.Exists || fileInfo.Length == seekTo) return;
		
						using (var fileStream = new FileStream(LogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (var reader = new StreamReader(fileStream))
						{
							reader.BaseStream.Seek(seekTo, SeekOrigin.Begin);
							string line;

							while ((line = reader.ReadLine()) != null)
								HandleMessage(ConsoleOut.Out(line));

							Interlocked.CompareExchange(ref seekTo, reader.BaseStream.Position, seekTo);
						}
					});

				this.Disposables.Add(disposable);
			}

			base.Start();
		}

		protected override void HandleMessage(ConsoleOut consoleOut)
		{
			var message = new KibanaMessage(consoleOut.Data);
			if (this.Started || string.IsNullOrWhiteSpace(message.Message)) return;
	
			if (!message.TryGetStartedConfirmation(out string host, out int? port)) return;
			this.Host = host;
			this.Port = port;
			this.BlockingSubject.OnNext(this.StartedHandle);
			this.Started = true;
			this.StartedHandle.Set();
		}
	}
}
