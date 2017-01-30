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
	public class ElasticsearchProcess : IDisposable
	{
		private ObservableProcess _process;
		private IDisposable _processListener;
		private readonly string[] _libs;

		public string JavaExe { get; }
		public string ElasticsearchJar { get; }
		public string HomeDirectory { get; private set; }
		public string ConfigDirectory { get; private set; }
		public string LibDirectory { get; }
		public string JavaOptions { get; }
		public bool Started { get; private set; }
		public int Port { get; private set; }
		public IEnumerable<string> AdditionalArguments { get; }
		public bool NoColor { get; private set; }
		private readonly Subject<ManualResetEvent> _blockingSubject = new Subject<ManualResetEvent>();

		public ElasticsearchProcess() : this(null) { }

		public ElasticsearchProcess(IEnumerable<string> args)
		{
			this.AdditionalArguments = ParseArgs(args);

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
			this.JavaExe = Path.Combine(javaHome, @"bin\java.exe");

			if (!File.Exists(this.JavaExe))
				throw new Exception("JAVA_HOME set but bin\\java.exe is not found using it as the root");
		}

		public void Start()
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

			this._process = new ObservableProcess(this.JavaExe, arguments.ToArray());

			//Create a hot observer on process that does not disposbe itself (Stop() method does this)
			var observable = Observable.Create<ConsoleOut>(observer =>
				{
					this._disposables.Add(this._process.Start().Subscribe(observer));
					return Disposable.Empty;
				}) 
				.Publish(); //promote to connectable observable

			//subscribe underlying observable stream
			this._disposables.Add(observable.Connect());

			if (Environment.UserInteractive)
			{
				//subscribe to all messages and write them to console
				this._disposables.Add(observable.Subscribe(c =>
				{
					if (c.Error) ElasticsearchConsole.WriteLine(ConsoleColor.Red, c.Data);
					else ElasticsearchConsole.WriteLine(c.Data);
				}));
			}

			//subscribe as long we are not in started state and attempt to read console out for this confirmation
			var handle = new ManualResetEvent(false);
			this._disposables.Add(observable
				.TakeWhile(c => !this.Started)
				.Select(consoleLine => new ElasticsearchMessage(this.Started, consoleLine.Data))
				.Subscribe(onNext: s => HandleConsoleMessage(s, handle))
			);

			var timeout = TimeSpan.FromSeconds(120);
			if (!handle.WaitOne(TimeSpan.FromSeconds(120), true))
			{
				this.Stop();
				throw new Exception($"Could not start Elasticsearch within ({timeout}): {this.JavaExe} {string.Join(" ", arguments)}");
			}
		}

		private void HandleConsoleMessage(ElasticsearchMessage s, ManualResetEvent handle)
		{
		
			if (this.Started || string.IsNullOrWhiteSpace(s.Message)) return;
			//if (s.Error && !this.Started)
			//{
			//	this.Fatal(handle, new Exception(consoleOut.Data));
			//	return;
			//}

			string version; int? pid; int port;
			if (s.TryParseNodeInfo(out version, out pid))
			{
			}
			else if (s.TryGetPortNumber(out port))
				this.Port = port;
			else if (s.TryGetStartedConfirmation())
			{
				this._blockingSubject.OnNext(handle);
				this.Started = true;
				handle.Set();
			}
		}

		private CompositeDisposable _disposables = new CompositeDisposable();
		public void Stop()
		{
			this._process?.Dispose();
			this._processListener?.Dispose();
			this._disposables?.Dispose();
			this._disposables = new CompositeDisposable();
		}

		public void Dispose()
		{
			this.Stop();
		}

		private List<string> ParseArgs(IEnumerable<string> args)
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
	}
}
