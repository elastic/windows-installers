using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
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

		public ManualResetEvent CompletionHandle { get; private set; } = new ManualResetEvent(false);
	}
}