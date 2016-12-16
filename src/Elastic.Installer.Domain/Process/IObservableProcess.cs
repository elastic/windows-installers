using System;

namespace Elastic.Installer.Domain.Process.ObservableWrapper
{
	public interface IObservableProcess : IDisposable
	{
		IObservable<ConsoleOut> Start();
		void Stop();
	}
}