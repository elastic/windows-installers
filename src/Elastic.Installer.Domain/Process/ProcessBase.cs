using Elastic.Installer.Domain.Process.ObservableWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Process
{
	public abstract class ProcessBase : IDisposable
	{
		protected ObservableProcess Process;
		protected ManualResetEvent StartedHandle { get; set; } = new ManualResetEvent(false);

		protected readonly Subject<ManualResetEvent> BlockingSubject = new Subject<ManualResetEvent>();
		protected CompositeDisposable Disposables { get; set; } = new CompositeDisposable();

		protected virtual List<string> ParseArguments(IEnumerable<string> args) => args?.ToList();
		protected virtual List<string> GetArguments() => this.AdditionalArguments;


		public string ProcessExe { get; protected set; }
		public bool Started { get; protected set; }
		public string HomeDirectory { get; protected set; }
		public string ConfigDirectory { get; protected set; }
		public List<string> AdditionalArguments { get; protected set; }


		public ProcessBase(IEnumerable<string> args)
		{
			this.AdditionalArguments = ParseArguments(args);
		}

		public virtual void Start()
		{
			this.Stop();

			var arguments = this.GetArguments();

			this.Process = new ObservableProcess(this.ProcessExe, arguments.ToArray());

			var observable = Observable.Create<ConsoleOut>(observer =>
				{
					this.Disposables.Add(this.Process.Start().Subscribe(observer));
					return Disposable.Empty;
				})
				.Publish(); //promote to connectable observable

			this.Disposables.Add(observable.Connect());

			if (Environment.UserInteractive)
			{
				//subscribe to all messages and write them to console
				this.Disposables.Add(observable.Subscribe(c =>
				{
					if (c.Error) WriteError(c.Data); else WriteSuccess(c.Data);
				}));
			}

			//subscribe as long we are not in started state and attempt to read console out for this confirmation
			this.Disposables.Add(observable
				.TakeWhile(c => !this.Started)
				.Subscribe(onNext: consoleOut => this.HandleMessage(consoleOut))
			);

			var timeout = TimeSpan.FromSeconds(120);
			if (!this.StartedHandle.WaitOne(TimeSpan.FromSeconds(120), true))
			{
				this.Stop();
				throw new Exception($"Could not start process within ({timeout}): {this.ProcessExe} {string.Join(" ", arguments)}");
			}
		}

		public virtual void Stop()
		{
			this.Process?.Dispose();
			this.Disposables?.Dispose();
			this.Disposables = new CompositeDisposable();
		}

		protected virtual void HandleMessage(ConsoleOut message) { }

		protected virtual void WriteError(string message) => Console.WriteLine(message);

		protected virtual void WriteSuccess(string message) => Console.WriteLine(message);

		public void Dispose() => this.Stop();
	}
}
