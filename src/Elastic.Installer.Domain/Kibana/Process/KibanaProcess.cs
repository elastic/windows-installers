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
	public class KibanaProcess : IDisposable
	{
		private ObservableProcess _process;
		private CompositeDisposable _disposables = new CompositeDisposable();

		public bool Started { get; private set; }
		public string NodeExe { get; private set; }
		public string HomeDirectory { get; private set; }
		public string ConfigDirectory { get; set; }
		public string JsPath { get; set; }
		public IEnumerable<string> AdditionalArguments { get; private set; }

		public KibanaProcess() : this(null) { }

		public KibanaProcess(IEnumerable<string> args)
		{
			this.AdditionalArguments = ParseArgs(args);

			this.HomeDirectory = (this.HomeDirectory 
				?? Environment.GetEnvironmentVariable("KIBANA_HOME", EnvironmentVariableTarget.Machine) 
				?? Directory.GetParent(".").FullName).TrimEnd('\\');

			this.ConfigDirectory = (this.ConfigDirectory 
				?? Environment.GetEnvironmentVariable("KIBANA_CONFIG", EnvironmentVariableTarget.Machine) 
				?? Path.Combine(this.HomeDirectory, "config")).TrimEnd('\\');

			this.JsPath = Path.Combine(this.HomeDirectory, @"src\cli");

			this.NodeExe = Path.Combine(this.HomeDirectory, @"node\node.exe");
		}

		public void Start()
		{
			this.Stop();

			var arguments = this.AdditionalArguments.Concat(new string[]
			{
				"--no-warnings",
				$"\"{this.JsPath}\"",
				$"--config \"{this.ConfigDirectory}\""
			})
			.ToList();

			this._process = new ObservableProcess(this.NodeExe, arguments.ToArray());

			var observable = Observable.Create<ConsoleOut>(observer =>
				{
					this._disposables.Add(this._process.Start().Subscribe(observer));
					return Disposable.Empty;
				}) 
				.Publish(); //promote to connectable observable

			this._disposables.Add(observable.Connect());

			if (Environment.UserInteractive)
			{
				//subscribe to all messages and write them to console
				this._disposables.Add(observable.Subscribe(c => Console.WriteLine(c.Data)));
			}

			//subscribe as long we are not in started state and attempt to read console out for this confirmation
			// TODO parse log output (which may be a file out stdout) to determine if Kibana was started on not
			//var handle = new ManualResetEvent(false);
			//this._disposables.Add(observable
			//	.TakeWhile(c => !this.Started)
			//	.Subscribe(onNext: consoleOut => HandleLogMessage(consoleOut))
			//);

			//var timeout = TimeSpan.FromSeconds(120);
			//if (!handle.WaitOne(TimeSpan.FromSeconds(120), true))
			//{
			//	this.Stop();
			//	throw new Exception($"Could not start Kibana within ({timeout}): {this.NodeExe} {string.Join(" ", arguments)}");
			//}
		}

		public void Stop()
		{
			this._process?.Dispose();
			this._disposables?.Dispose();
			this._disposables = new CompositeDisposable();
		}

		public void Dispose()
		{
			this.Stop();
		}

		private void HandleLogMessage(ConsoleOut consoleOut)
		{

		}

		private List<string> ParseArgs(IEnumerable<string> args)
		{
			var newArgs = new List<string>();
			if (args == null)
				return newArgs;
			var nextArgIsConfigPath = false;
			foreach(var arg in args)
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
	}
}
