﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace Elastic.ProcessHosts.Process
{
	public enum RunningState
	{
		Stopped,
		Stopping,
		Starting,
		AssumedStarted,
		ConfirmedStarted
	}

	public abstract class ProcessBase : IDisposable
	{
		protected IFileSystem FileSystem { get; }
		private readonly IConsoleOutHandler _handler;
		private readonly IObservableProcess _process;

		public int? LastExitCode => this._process?.LastExitCode;

		protected string ProcessExe { get; set; }
		protected IEnumerable<string> Arguments { private get; set; }
		public bool Started => this.RunningState == RunningState.AssumedStarted || this.RunningState == RunningState.ConfirmedStarted;
		public RunningState RunningState { get; protected set; }
		protected string HomeDirectory { get; set; }
		protected string ConfigDirectory { get; set; }

		protected ManualResetEvent StartedHandle { get; private set; }
		public ManualResetEvent CompletedHandle { get; private set; }

		protected CompositeDisposable Disposables { get; private set; } = new CompositeDisposable();

		protected ProcessBase(
			IObservableProcess process,
			IConsoleOutHandler handler,
			IFileSystem fileSystem,
			ManualResetEvent completedHandle = null)
		{
			this.FileSystem = fileSystem ?? new FileSystem();
			this._process = process ?? new ObservableProcess();
			this._handler = handler ?? new ConsoleOutHandler();
			this.CompletedHandle = completedHandle;
			this.RunningState = RunningState.Stopped;
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
			this.RunningState = RunningState.Starting;
			this.StartedHandle = new ManualResetEvent(false);

			var bin = this.ProcessExe;
			var args = this.Arguments;

			try
			{
				var observable = this._process.Start(bin, args).Publish();
				if (this._process.UserInteractive)
				{
					//subscribe to all messages and write them to console
					this.Disposables.Add(observable.Subscribe(this._handler.Write, delegate { }, delegate { }));
				}

				//subscribe as long we are not in started state and attempt to read console
				//out for this confirmation
				this.Disposables.Add(observable
					.TakeWhile(c => !this.Started)
					.Subscribe(this.Handle, delegate { }, delegate { })
				);
				this.Disposables.Add(observable.Subscribe(delegate { }, HandleException, HandleCompleted));
				this.Disposables.Add(observable.Connect());

				var timeout = this._process.WaitForStarted;
				//we wait for 1 minute to attempt a clean start (meaning when the service is started elasticsearch is started)
				//this is a best effort, elasticsearch could take longer to start if it needs to relocate shards for instance
				this.StartedHandle.WaitOne(timeout, true);

				if (this.RunningState != RunningState.ConfirmedStarted) this.RunningState = RunningState.AssumedStarted;
			}
			catch
			{
				this.Stop();
				throw;
			}
		}

		public void Stop()
		{
			this.RunningState = RunningState.Stopping;
			this.CompletedHandle?.Reset();
			this._process?.Dispose();
			this.Disposables?.Dispose();
			this.Disposables = new CompositeDisposable();
			this.RunningState = RunningState.Stopped;
		}

		private void HandleException(Exception e)
		{
			this.CompletedHandle?.Set();
			this.StartedHandle?.Set();
			throw e;
		}

		private void HandleCompleted()
		{
			this.StartedHandle?.Set();
			this.CompletedHandle?.Set();
		}

		private void Handle(ConsoleOut message)
		{
			this._handler.Handle(message);
			this.HandleMessage(message);
		}

		protected virtual void HandleMessage(ConsoleOut message)
		{
		}

		public void Dispose() => this.Stop();
	}
}