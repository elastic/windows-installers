using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;

namespace Elastic.ProcessHosts.Process
{
	public class ObservableProcess : IObservableProcess
	{
		private readonly object _lock = new object();

		private System.Diagnostics.Process Process { get; set; }
		private bool Started { get; set; }
		public int LastExitCode { get; private set; }
		public bool UserInteractive => Environment.UserInteractive;
		public TimeSpan WaitForStarted => TimeSpan.FromMinutes(2);

		private IObservable<ConsoleOut> OutStream { get; set; }

		public IObservable<ConsoleOut> Start(string binary, IEnumerable<string> args)
		{
			if (this.Started) return OutStream ?? Observable.Empty<ConsoleOut>();
			lock (_lock)
			{
				if (this.Started) return OutStream ?? Observable.Empty<ConsoleOut>();
				this.Process = CreateProcess(binary, args?.ToArray());
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

		private IDisposable CreateProcessExitSubscription(System.Diagnostics.Process process, IObservable<EventPattern<object>> processExited,
			IObserver<ConsoleOut> observer)
		{
			return processExited.Subscribe(args =>
			{
				try
				{
					this.LastExitCode = process?.ExitCode ?? 0;

					//if process does not terminated with 0 (no errors) or 130 (cancellation requested on java.exe)
					//throw an exception that bubbles back out to Program (elasticsearch.exe)
					if (this.LastExitCode > 0 && this.LastExitCode != 130)
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
			}, e => { observer.OnCompleted(); }, observer.OnCompleted);
		}

		private void CallSelfToStop()
		{
			var exe = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
			var p = CreateProcess(exe, "--clean-shutdown", this.Process.Id.ToString(CultureInfo.InvariantCulture));
			//we wait 5 minutes for a clean shutdown
			if(p.Start())
				this.Process?.WaitForExit((int)TimeSpan.FromMinutes(5).TotalMilliseconds);
		}

		private static System.Diagnostics.Process CreateProcess(string exe, params string[] args)
		{
			var p = new System.Diagnostics.Process
			{
				EnableRaisingEvents = true,
				StartInfo =
				{
					EnvironmentVariables = { {"HOSTNAME", Environment.MachineName } },
					FileName = exe,
					Arguments = args != null ? string.Join(" ", args) : string.Empty,
					CreateNoWindow = true,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = false,
				}
			};
			return p;
		}

		public void Stop()
		{
			try
			{
				if (this.Process == null) return;
				this.CallSelfToStop();

				this.Process?.Kill();
				this.Process?.WaitForExit(2000);
				this.Process?.Close();
			}
			catch (InvalidOperationException)
			{
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