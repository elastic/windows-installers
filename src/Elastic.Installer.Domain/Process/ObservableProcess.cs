using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Elastic.Installer.Domain.Extensions;

namespace Elastic.Installer.Domain.Process
{
	public class ObservableProcess : IObservableProcess
	{

		//import in the declaration for GenerateConsoleCtrlEvent
		[DllImport("kernel32.dll", SetLastError=true)]
		static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);
		public enum ConsoleCtrlEvent
		{
			CTRL_C = 0,
			CTRL_BREAK = 1,
			CTRL_CLOSE = 2,
			CTRL_LOGOFF = 5,
			CTRL_SHUTDOWN = 6
		}

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
                        throw new StartupException($"Failed to start observable process: {binary}");

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
						observer.OnError(new StartupException(
							$"Process '{process.StartInfo.FileName}' terminated with error code {process.ExitCode}"));
					}
					else observer.OnCompleted();
				}
				finally
				{
					this.Stop();
				}
			}, e => { observer.OnCompleted(); }, observer.OnCompleted)
			;
		}

		public void Stop()
		{
			try
			{
				if (this.Process == null) return;
				Console.WriteLine("sending ctrl+c");
				GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_C, 0);
				this.Process.WaitForExit(5000);
				Console.WriteLine("sending ctrl+break");
				GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_BREAK, 0);
				Console.WriteLine("sending ctrl+close");
				GenerateConsoleCtrlEvent(ConsoleCtrlEvent.CTRL_CLOSE, 0);
				this.Process.WaitForExit(5000);
				if (this.Process.HasExited)
				{
					this.Process.Close();
					return;
				}
				Console.WriteLine("process kill");
				this.Process.Kill();
				this.Process.WaitForExit(2000);
				this.Process.Close();
			}
			catch (InvalidOperationException){ }
			finally
			{
				this.Started = false;
				this.OutStream = null;
			}
		}

		public void Dispose() => this.Stop();
	}
}