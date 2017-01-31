using Elastic.Installer.Domain.Process;
using Elastic.Installer.Domain.Process.ObservableWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Process
{
	public class KibanaProcess : ProcessBase
	{
		public string JsPath { get; set; }

		public KibanaProcess() : this(null) { }

		public KibanaProcess(IEnumerable<string> args) : base(args)
		{
			this.HomeDirectory = (this.HomeDirectory
				?? Environment.GetEnvironmentVariable("KIBANA_HOME", EnvironmentVariableTarget.Machine)
				?? Directory.GetParent(".").FullName).TrimEnd('\\');

			this.ConfigDirectory = (this.ConfigDirectory
				?? Environment.GetEnvironmentVariable("KIBANA_CONFIG", EnvironmentVariableTarget.Machine)
				?? Path.Combine(this.HomeDirectory, "config")).TrimEnd('\\');

			this.JsPath = Path.Combine(this.HomeDirectory, @"src\cli");

			this.ProcessExe = Path.Combine(this.HomeDirectory, @"node\node.exe");
		}

		protected override List<string> GetArguments()
		{
			var arguments = this.AdditionalArguments.Concat(new string[]
			{
				"--no-warnings",
				$"\"{this.JsPath}\"",
				$"--config \"{this.ConfigDirectory}\""
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
				else if (nextArgIsConfigPath)
				{
					nextArgIsConfigPath = false;
					this.ConfigDirectory = arg;
				}
				else
					newArgs.Add(arg);

			}
			return newArgs;
		}

		protected override void HandleMessage(ConsoleOut consoleOut)
		{
			// TODO parse log output (either stdout or file) to determine if Kibana has started
			this.BlockingSubject.OnNext(this.StartedHandle);
			this.Started = true;
			this.StartedHandle.Set();
		}
	}
}
