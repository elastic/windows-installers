using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Elastic.Installer.Domain.Process
{
	public abstract class ProcessBase : IDisposable
	{
		protected IFileSystem FileSystem { get; }
		private readonly IConsoleOutHandler _handler;
		private readonly IObservableProcess _process;

		protected string ProcessExe { get; set; }
		protected IEnumerable<string> Arguments { private get; set; }
		protected bool Started { get; set; }
		protected string HomeDirectory { get; set; }
		protected string ConfigDirectory { get; set; }

		protected ManualResetEvent StartedHandle { get; } = new ManualResetEvent(false);
		protected readonly Subject<ManualResetEvent> BlockingSubject = new Subject<ManualResetEvent>();
		protected CompositeDisposable Disposables { get; private set; } = new CompositeDisposable();

		protected ProcessBase(IObservableProcess process, IConsoleOutHandler handler, IFileSystem fileSystem)
		{
			this.FileSystem = fileSystem ?? new FileSystem();
			this._process = process ?? new ObservableProcess();
			this._handler = handler ?? new ConsoleOutHandler();
		}

		/// <summary>
		/// Yield the actual arguments to the wrapped process
		/// </summary>
		/// <param name="arguments">The sanitized list of arguments to the outer process</param>
		protected abstract IEnumerable<string> CreateObservableProcessArguments(IEnumerable<string> arguments);

		/// <summary>
		/// Arguments to the process itself
		/// </summary>
		/// <param name="arguments">Sanitized list of arguments</param>
		protected abstract IEnumerable<string> ParseArguments(IEnumerable<string> arguments);

		public virtual void Start()
		{
			this.Stop();

			var bin = this.ProcessExe;
			var args = this.Arguments;

			var observable = this._process.Start(bin, args).Publish();
			if (this._process.UserInteractive)
			{
				//subscribe to all messages and write them to console
				this.Disposables.Add(observable.Subscribe(this._handler.Write));
			}

			//subscribe as long we are not in started state and attempt to read console out for this confirmation
			this.Disposables.Add(observable
				.TakeWhile(c => !this.Started)
				.Subscribe(this.Handle)
			);

			this.Disposables.Add(observable.Connect());

			var timeout = this._process.WaitForStarted;
			if (this.StartedHandle.WaitOne(timeout, true)) return;

			this.Stop();
			throw new Exception($"Could not start process within ({timeout}): {bin} {string.Join(" ", args)}");
		}

		public void Stop()
		{
			this._process?.Dispose();
			this.Disposables?.Dispose();
			this.Disposables = new CompositeDisposable();
		}

		private void Handle(ConsoleOut message)
		{
			this._handler.Handle(message);
			this.HandleMessage(message);
		}
		protected virtual void HandleMessage(ConsoleOut message) { }

		public void Dispose() => this.Stop();
	}
}
