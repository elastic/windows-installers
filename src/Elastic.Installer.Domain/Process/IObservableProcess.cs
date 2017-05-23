using System;
using System.Collections.Generic;

namespace Elastic.Installer.Domain.Process
{
	public interface IObservableProcess : IDisposable
	{
		IObservable<ConsoleOut> Start(string binary, IEnumerable<string> args);
		void Stop();

		bool UserInteractive { get; }
		int LastExitCode { get; }

		TimeSpan WaitForStarted { get; }
	}
}