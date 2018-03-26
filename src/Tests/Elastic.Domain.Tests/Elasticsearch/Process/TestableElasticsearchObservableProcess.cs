using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Elastic.Configuration.EnvironmentBased;
using Elastic.ProcessHosts.Elasticsearch.Process;
using Elastic.ProcessHosts.Process;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
	public class TestableElasticsearchObservableProcess : IObservableProcess
	{
		private readonly ConsoleSession _session;
		public bool UserInteractive { get; }
		public int LastExitCode { get; }
		public TimeSpan WaitForStarted => TimeSpan.FromSeconds(.2);

		public bool StopCalled { get; private set; }
		public bool StartCalled { get; private set; }
		public bool DisposeCalled { get; private set; }
		public string BinaryCalled { get; private set; }
		public string[] ArgsCalled { get; private set; }
		public Dictionary<string, string> ProcessVariables { get; }

		public static string EndProcess { get; } = "-END-";
		public static string ThrowException { get; } = "-EXCEPTION-";
		public static string ThrowStartupException { get; } = "-STARTUP EXCEPTION-";

		public TestableElasticsearchObservableProcess(ElasticsearchEnvironmentConfiguration env, ConsoleSession session, bool interactive = true)
		{
			_session = session;
			UserInteractive = interactive;
			this.ProcessVariables = new Dictionary<string, string>();
			ElasticsearchObservableProcess.AlterProcessVariables(env, this.ProcessVariables, warn: false);
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