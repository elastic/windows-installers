using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Elastic.ProcessHosts.Process
{

	public static class RxProcessUtilities
	{
		public static IObservable<ConsoleOut> CreateStandardErrorObservable(this System.Diagnostics.Process process)
		{
			var receivedStdErr =
				Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>
					(h => process.ErrorDataReceived += h, h => process.ErrorDataReceived -= h)
				.Select(e => ConsoleOut.ErrorOut(e.EventArgs.Data));

			return Observable.Create<ConsoleOut>(observer =>
			{
				var cancel = Disposable.Create(process.CancelErrorRead);
				return new CompositeDisposable(cancel, receivedStdErr.Subscribe(observer));
			});
		}

		public static IObservable<ConsoleOut> CreateStandardOutputObservable(this System.Diagnostics.Process process)
		{
			var receivedStdOut =
				Observable.FromEventPattern<DataReceivedEventHandler, DataReceivedEventArgs>
					(h => process.OutputDataReceived += h, h => process.OutputDataReceived -= h)
				.Select(e => ConsoleOut.Out(e.EventArgs.Data));

			return Observable.Create<ConsoleOut>(observer =>
			{
				var cancel = Disposable.Create(process.CancelOutputRead);
				return new CompositeDisposable(cancel, receivedStdOut.Subscribe(observer));
			});
		}
	}

}