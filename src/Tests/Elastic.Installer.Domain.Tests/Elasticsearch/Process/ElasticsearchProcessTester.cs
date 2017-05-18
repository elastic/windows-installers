using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Process;
using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
	public class ElasticsearchProcessTester
	{
		public ElasticsearchProcess Process { get; set; }
		public TestableElasticsearchConsoleOutHandler OutHandler { get; set; }
		public TestableElasticsearchObservableProcess ObservableProcess { get; set; }

		public static string DefaultJavaHome {get;} = @"C:\Java";
		public static  string DefaultEsHome {get;}= LocationsModel.DefaultInstallationDirectory;

		private ElasticsearchProcessTester(Func<ElasticsearchProcessTesterStateProvider, ElasticsearchProcessTesterStateProvider> setup)
		{
			var state = setup(new ElasticsearchProcessTesterStateProvider());

			this.ObservableProcess = new TestableElasticsearchObservableProcess(state.ConsoleSessionState, state.InteractiveEnvironment);
			this.OutHandler = state.OutHandler;

			this.Process = new ElasticsearchProcess(
				this.ObservableProcess,
				this.OutHandler,
				state.FileSystemState,
				state.ElasticsearchState,
				state.JavaConfigState, null);
		}

		public static ElasticsearchProcessTester AllDefaults() => new ElasticsearchProcessTester(s=>s
			.Elasticsearch(e=>e.HomeDirectoryEnvironmentVariable(DefaultEsHome))
			.Java(j=>j.JavaHomeMachine(DefaultJavaHome))
			.ConsoleSession(ConsoleSession.Valid)
			.FileSystem(s.FileSystemDefaults)
		);

		public void Start(Action<ElasticsearchProcessTester> assert)
		{
			this.Process.Start();
			assert(this);
		}
	}

	public class ElasticsearchProcessTesterStateProvider
	{
		public ElasticsearchProcessTesterStateProvider Java(Func<MockJavaEnvironmentStateProvider, MockJavaEnvironmentStateProvider> setter)
		{
			this.JavaState = setter(new MockJavaEnvironmentStateProvider());
			this.JavaConfigState = new JavaConfiguration(this.JavaState);
			return this;
		}

		public ElasticsearchProcessTesterStateProvider Elasticsearch(Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> setter)
		{
			this.ElasticsearchState = setter(new MockElasticsearchEnvironmentStateProvider());
			return this;
		}

		public ElasticsearchProcessTesterStateProvider FileSystem(Func<MockFileSystem, MockFileSystem> selector)
		{
			this.FileSystemState = selector(new MockFileSystem());
			return this;
		}

		public MockFileSystem FileSystemDefaults(MockFileSystem fs)
		{
			var java = new JavaConfiguration(this.JavaState);
			fs.AddFile(java.JavaExecutable, new MockFileData(""));
			var libFolder = Path.Combine(this.ElasticsearchState.HomeDirectory, @"lib");
			fs.AddDirectory(libFolder);
			fs.AddFile(Path.Combine(libFolder, @"elasticsearch-5.0.0.jar"), new MockFileData(""));
			fs.AddFile(Path.Combine(libFolder, @"dep1.jar"), new MockFileData(""));
			fs.AddFile(Path.Combine(libFolder, @"dep2.jar"), new MockFileData(""));
			fs.AddFile(Path.Combine(libFolder, @"dep3.jar"), new MockFileData(""));
			fs.AddFile(Path.Combine(libFolder, @"dep4.jar"), new MockFileData(""));
			return fs;
		}

		public ElasticsearchProcessTesterStateProvider ConsoleSession(ConsoleSession session)
		{
			this.ConsoleSessionState = session;
			return this;
		}
		public ElasticsearchProcessTesterStateProvider Interactive(bool interactive = true)
		{
			this.InteractiveEnvironment = interactive;
			return this;
		}

		public TestableElasticsearchConsoleOutHandler OutHandler { get; } = new TestableElasticsearchConsoleOutHandler();

		public bool InteractiveEnvironment { get; private set; }

		public ConsoleSession ConsoleSessionState { get; private set; } = new ConsoleSession();

		public MockFileSystem FileSystemState { get; private set; } = new MockFileSystem();

		public MockElasticsearchEnvironmentStateProvider ElasticsearchState { get; private set; } = new MockElasticsearchEnvironmentStateProvider();

		public MockJavaEnvironmentStateProvider JavaState { get; private set; } = new MockJavaEnvironmentStateProvider();
		public JavaConfiguration JavaConfigState { get; private set; }
	}
}