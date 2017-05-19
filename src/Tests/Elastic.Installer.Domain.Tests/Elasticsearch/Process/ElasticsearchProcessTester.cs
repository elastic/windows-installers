using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
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

		public static string DefaultJavaHome { get; } = @"C:\Java";
		public static string DefaultEsHome { get; } = LocationsModel.DefaultInstallationDirectory;

		private ElasticsearchProcessTester(Func<ElasticsearchProcessTesterStateProvider, ElasticsearchProcessTesterStateProvider> setup)
		{
			var state = setup(new ElasticsearchProcessTesterStateProvider());

			this.ObservableProcess = new TestableElasticsearchObservableProcess(state.ConsoleSessionState, state.InteractiveEnvironment);
			this.OutHandler = state.OutHandler;

			this.Process = new ElasticsearchProcess(
				this.ObservableProcess,
				this.OutHandler,
				state.FileSystemState,
				state.ElasticsearchConfigState,
				state.JavaConfigState, state.ProcessArgs);
		}

		public static ElasticsearchProcessTester AllDefaults(params string[] args) => new ElasticsearchProcessTester(s => s
			.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
			.Java(j => j.JavaHomeMachine(DefaultJavaHome))
			.ConsoleSession(ConsoleSession.Valid)
			.FileSystem(s.FileSystemDefaults)
			.ProcessArguments(args)
		);

		public static ElasticsearchProcessTester JavaChangesOnly(
			Func<MockJavaEnvironmentStateProvider, MockJavaEnvironmentStateProvider> javaState) => new ElasticsearchProcessTester(s => s
			.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
			.Java(javaState)
			.ConsoleSession(ConsoleSession.Valid)
			.FileSystem(s.FileSystemDefaults)
		);

		public static ElasticsearchProcessTester ElasticsearchChangesOnly(
			Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> esState,
			params string[] args) => ElasticsearchChangesOnly(null, esState, args);

		public static ElasticsearchProcessTester ElasticsearchChangesOnly(
			string homeDirectoryForFileSystem,
			Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> esState,
			params string[] args) =>
			new ElasticsearchProcessTester(s => s
				.Elasticsearch(esState)
				.Java(j => j.JavaHomeMachine(DefaultJavaHome))
				.ConsoleSession(ConsoleSession.Valid)
				.FileSystem(fs=> s.AddJavaExe(s.AddElasticsearchLibs(fs, homeDirectoryForFileSystem)))
				.ProcessArguments(args)
			);

		public static ElasticsearchProcessTester Create(Func<ElasticsearchProcessTesterStateProvider, ElasticsearchProcessTesterStateProvider> setup) =>
			new ElasticsearchProcessTester(setup);

		public static void CreateThrows(
			Func<ElasticsearchProcessTesterStateProvider, ElasticsearchProcessTesterStateProvider> setup,
			Action<Exception> assert)
		{
			var created = false;
			try
			{
				var elasticsearchProcessTester = new ElasticsearchProcessTester(setup);
				created = true;
			}
			catch (Exception e)
			{
				assert(e);
			}
			if (created)
				throw new Exception("Process tester expected elasticsearch.exe to throw an exception");
		}

		public void Start(Action<ElasticsearchProcessTester> assert)
		{
			this.Process.Start();
			assert(this);
		}

		public void StartThrows(Action<Exception, ElasticsearchProcessTester> assert)
		{
			var started = false;
			try
			{
				this.Process.Start();
				started = true;
			}
			catch (Exception e)
			{
				assert(e, this);
			}
			if (started)
				throw new Exception("Process tester expected elasticsearch.exe to throw an exception");
		}
	}

	public class ElasticsearchProcessTesterStateProvider
	{
		public MockJavaEnvironmentStateProvider JavaState { get; private set; }
		public JavaConfiguration JavaConfigState { get; private set; }

		public MockElasticsearchEnvironmentStateProvider ElasticsearchState { get; private set; }
		public ElasticsearchEnvironmentConfiguration ElasticsearchConfigState { get; private set; }

		public ElasticsearchProcessTesterStateProvider()
		{
			this.JavaState = new MockJavaEnvironmentStateProvider();
			this.JavaConfigState = new JavaConfiguration(this.JavaState);

			this.ElasticsearchState = new MockElasticsearchEnvironmentStateProvider();
			this.ElasticsearchConfigState = new ElasticsearchEnvironmentConfiguration(this.ElasticsearchState);
		}

		public ElasticsearchProcessTesterStateProvider Java(Func<MockJavaEnvironmentStateProvider, MockJavaEnvironmentStateProvider> setter)
		{
			this.JavaState = setter(new MockJavaEnvironmentStateProvider());
			this.JavaConfigState = new JavaConfiguration(this.JavaState);
			return this;
		}

		public ElasticsearchProcessTesterStateProvider Elasticsearch(
			Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> setter)
		{
			this.ElasticsearchState = setter(new MockElasticsearchEnvironmentStateProvider());
			this.ElasticsearchConfigState = new ElasticsearchEnvironmentConfiguration(this.ElasticsearchState);
			return this;
		}

		public ElasticsearchProcessTesterStateProvider FileSystem(Func<MockFileSystem, MockFileSystem> selector)
		{
			this.FileSystemState = selector(new MockFileSystem());
			return this;
		}

		public MockFileSystem FileSystemDefaults(MockFileSystem fs) => AddJavaExe(AddElasticsearchLibs(fs));

		public MockFileSystem AddJavaExe(MockFileSystem fs)
		{
			var java = new JavaConfiguration(this.JavaState);
			fs.AddFile(java.JavaExecutable, new MockFileData(""));
			return fs;
		}

		public MockFileSystem AddElasticsearchLibs(MockFileSystem fs) => this.AddElasticsearchLibs(fs, null);
		public MockFileSystem AddElasticsearchLibs(MockFileSystem fs, string home)
		{
			var libFolder = Path.Combine(home ?? this.ElasticsearchConfigState.HomeDirectory, @"lib");
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

		public ElasticsearchProcessTesterStateProvider ProcessArguments(params string[] args)
		{
			this.ProcessArgs = args;
			return this;
		}

		public TestableElasticsearchConsoleOutHandler OutHandler { get; } = new TestableElasticsearchConsoleOutHandler();

		public bool InteractiveEnvironment { get; private set; }

		public string[] ProcessArgs { get; private set; }

		public ConsoleSession ConsoleSessionState { get; private set; } = new ConsoleSession();

		public MockFileSystem FileSystemState { get; private set; } = new MockFileSystem();
	}
}