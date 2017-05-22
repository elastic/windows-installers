using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Elastic.Installer.Domain.Process;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
	public class TestableElasticsearchObservableProcess : IObservableProcess
	{
		private readonly ConsoleSession _session;
		public bool UserInteractive { get; }
		public TimeSpan WaitForStarted => TimeSpan.FromSeconds(.2);

		public bool StopCalled { get; private set; }
		public bool StartCalled { get; private set; }
		public bool DisposeCalled { get; private set; }
		public string BinaryCalled { get; private set; }
		public string[] ArgsCalled { get; private set; }

		public static string EndProcess { get; } = "-END-";
		public static string ThrowException { get; } = "-EXCEPTION-";
		public static string ThrowStartupException { get; } = "-STARTUP EXCEPTION-";

		public TestableElasticsearchObservableProcess(ConsoleSession session, bool interactive = true)
		{
			_session = session;
			UserInteractive = interactive;
		}

		public void Dispose()
		{
			this.DisposeCalled = true;
		}

		public IObservable<ConsoleOut> Start(string binary, IEnumerable<string> args)
		{
			this.StartCalled = true;

			this.BinaryCalled = binary;
			this.ArgsCalled = args?.ToArray();

			return Observable.Create<ConsoleOut>(o => {
				foreach (var c in this._session)
				{
					if (c.Data == EndProcess)
						o.OnCompleted();
					else if (c.Data == ThrowException)
						o.OnError(new Exception("Process did something funky here"));
					else if (c.Data == ThrowStartupException)
						o.OnError(new StartupException("Process did something funky here"));
					else o.OnNext(c);
				}
				return Disposable.Empty;
			});
		}

		public void Stop()
		{
			this.StopCalled = true;
		}
	}
}