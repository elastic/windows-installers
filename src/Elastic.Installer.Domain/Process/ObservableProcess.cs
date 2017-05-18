using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Elastic.Installer.Domain.Process
{
	public class ObservableProcess : IObservableProcess
	{
		public System.Diagnostics.Process Process { get; private set; }
		private readonly object _lock = new object();

		private bool Started { get; set; }
		public bool UserInteractive => Environment.UserInteractive;
		public TimeSpan WaitForStarted => TimeSpan.FromMinutes(2);

		private IObservable<ConsoleOut> OutStream { get; set; }

		public IObservable<ConsoleOut> Start(string binary, IEnumerable<string> args)
		{
			if (this.Started) return OutStream ?? Observable.Empty<ConsoleOut>();
			lock(_lock)
			{
				if (this.Started) return OutStream ?? Observable.Empty<ConsoleOut>();
                this.Process = new System.Diagnostics.Process
                {
                    EnableRaisingEvents = true,
                    StartInfo =
                    {
                        FileName = binary,
                        Arguments = string.Join(" ", args),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = false
                    }
                };
                this.OutStream = Observable.Create<ConsoleOut>(observer =>
                {
                    // listen to stdout and stderr
                    var stdOut = this.Process.CreateStandardOutputObservable();
                    var stdErr = this.Process.CreateStandardErrorObservable();

                    var stdOutSubscription = stdOut.Subscribe(observer);
                    var stdErrSubscription = stdErr.Subscribe(observer);

                    var processExited = Observable.FromEventPattern(h => this.Process.Exited += h, h => this.Process.Exited -= h);
                    var processError = CreateProcessExitSubscription(this.Process, processExited, observer);

                    if (!this.Process.Start())
                        throw new Exception($"Failed to start observable process: {binary}");

                    this.Process.BeginOutputReadLine();
                    this.Process.BeginErrorReadLine();
                    this.Started = true;

                    return new CompositeDisposable(stdOutSubscription, stdErrSubscription, processError);
                });
				return this.OutStream;

			}
		}

		private IDisposable CreateProcessExitSubscription(System.Diagnostics.Process process, IObservable<EventPattern<object>> processExited, IObserver<ConsoleOut> observer)
		{
			return processExited.Subscribe(args =>
			{
				try
				{
					if (process?.ExitCode > 0)
					{
						observer.OnError(new Exception(
							$"Process '{process.StartInfo.FileName}' terminated with error code {process.ExitCode}"));
					}
					else observer.OnCompleted();
				}
				finally
				{
					this.Stop();
				}
			});
		}

		public void Stop()
		{
            try
            {
                this.Process?.Kill();
                this.Process?.WaitForExit(2000);
                this.Process?.Close();
            }
            finally
            {
	            this.Started = false;
	            this.OutStream = null;
            }
		}

		public void Dispose() => this.Stop();
	}
}