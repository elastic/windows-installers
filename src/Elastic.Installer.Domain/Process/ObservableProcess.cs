using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Elastic.Installer.Domain.Process.ObservableWrapper
{
	public class ObservableProcess : IObservableProcess
	{
		public ObservableProcess(string bin, params string[] args)
		{
			this.Binary = bin;
			this.Arguments = string.Join(" ", args);

			this.Process = new System.Diagnostics.Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					FileName = this.Binary,
					Arguments = this.Arguments,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = false
				}
			};
		}

		private bool Started { get; set; }

		public int? ExitCode { get; private set; }

		public string Binary { get; }

		public System.Diagnostics.Process Process { get; }

		public string Arguments { get; }

		public IObservable<ConsoleOut> Start()
		{
			return Observable.Create<ConsoleOut>(observer =>
			{
				// listen to stdout and stderr
				var stdOut = this.Process.CreateStandardOutputObservable();
				var stdErr = this.Process.CreateStandardErrorObservable();

				var stdOutSubscription = stdOut.Subscribe(observer);
				var stdErrSubscription = stdErr.Subscribe(observer);

				var processExited = Observable.FromEventPattern(h => this.Process.Exited += h, h => this.Process.Exited -= h);
				var processError = CreateProcessExitSubscription(this.Process, processExited, observer);

				if (!this.Process.Start())
					throw new Exception($"Failed to start observable process: {this.Binary}");

				this.Process.BeginOutputReadLine();
				this.Process.BeginErrorReadLine();
				this.Started = true;

				return new CompositeDisposable(stdOutSubscription, stdErrSubscription, processError);
			});
		}

		private IDisposable CreateProcessExitSubscription(System.Diagnostics.Process process, IObservable<EventPattern<object>> processExited, IObserver<ConsoleOut> observer)
		{
			return processExited.Subscribe(args =>
			{
				try
				{
					this.ExitCode = process?.ExitCode;
					if (process?.ExitCode > 0)
					{
						observer.OnError(new Exception(
							$"Process '{process.StartInfo.FileName}' terminated with error code {process.ExitCode}"));
					}
					else observer.OnCompleted();
				}
				finally
				{
					this.Started = false;
					process?.Close();
				}
			});
		}

		public void Stop()
		{
			if (this.Started)
			{
				try
				{
					this.Process?.Kill();
					this.Process?.WaitForExit(2000);
					this.Process?.Close();
				}
				catch (Exception)
				{
				}
			}
			this.Started = false;
		}

		public void Dispose() => this.Stop();
	}
}