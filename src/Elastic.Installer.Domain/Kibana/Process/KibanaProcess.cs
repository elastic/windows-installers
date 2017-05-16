using Elastic.Installer.Domain.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Elastic.Installer.Domain.Kibana.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Kibana.Configuration.FileBased;

namespace Elastic.Installer.Domain.Kibana.Process
{
	public class KibanaProcess : ProcessBase
	{
		public string JsPath { get; set; }

		public string ConfigFile { get; set; }

		public string Host { get; set; }

		public string LogFile { get; set; }

		public int? Port { get; set; }

		public KibanaProcess() : this(null) { }

		public KibanaProcess(IEnumerable<string> args) : base(args)
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
		}

		protected override List<string> GetArguments()
		{
			var arguments = this.AdditionalArguments.Concat(new[]
			{
				"--no-warnings",
				$"\"{this.JsPath}\"",
				$"--config \"{this.ConfigFile}\""
			})
			.ToList();

			return arguments;
		}

		protected override List<string> ParseArguments(IEnumerable<string> args)
		{
			var newArgs = new List<string>();
			if (args == null)
				return newArgs;
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
	
			string host; int? port;
		    if (message.TryGetStartedConfirmation(out host, out port))
		    {
			    this.Host = host;
			    this.Port = port;
				this.BlockingSubject.OnNext(this.StartedHandle);
				this.Started = true;
				this.StartedHandle.Set();
			}
		}
	}
}
