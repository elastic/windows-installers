using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Configuration;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{

	public class TestSetupStateProvider
	{
		public TestSetupStateProvider Java(Func<MockJavaEnvironmentStateProvider, MockJavaEnvironmentStateProvider> setter)
		{
			this.JavaState = setter(new MockJavaEnvironmentStateProvider());
			return this;
		}

		public TestSetupStateProvider Elasticsearch(Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> setter)
		{
			this.ElasticsearchState = setter(new MockElasticsearchEnvironmentStateProvider());
			return this;
		}

		public TestSetupStateProvider ServicePreviouslyInstalled()
		{
			this.ServiceState = new NoopServiceStateProvider() { SeesService = true };
			return this;
		}

		public TestSetupStateProvider PreviouslyInstalledPlugins(params string[] previouslyInstalled)
		{
			this.PluginState = new NoopPluginStateProvider(previouslyInstalled) { };
			return this;
		}

		public TestSetupStateProvider Wix(string currentVersion, string existingVersion)
		{
			this.WixState = new MockWixStateProvider() { CurrentVersion = currentVersion };
			if (!string.IsNullOrWhiteSpace(existingVersion)) this.WixState.ExistingVersion = existingVersion;
			return this;
		}

		public TestSetupStateProvider Wix(bool alreadyInstalled)
		{
			var currentVersion = "5.0.0";
			this.WixState = new MockWixStateProvider() { CurrentVersion = currentVersion };
			if (alreadyInstalled) this.WixState.ExistingVersion = currentVersion;
			return this;
		}

		public TestSetupStateProvider FileSystem(Func<MockFileSystem, MockFileSystem> selector)
		{
			this.FileSystemState = selector(new MockFileSystem());
			return this;
		}

		public TestSetupStateProvider SetupArgument(string variable, string value)
		{
			var v = variable.Split('.').Last().ToUpperInvariant();
			this.IntermediateArguments.Add($"{v}=\"{value}\"");
			return this;
		}

		public TestSetupStateProvider Session(bool uninstalling = true)
		{
			this.SessionState = new NoopSession { Uninstalling = true };
			return this;
		}

		public MockFileSystem FileSystemState { get; private set; } = new MockFileSystem();

		public MockWixStateProvider WixState { get; private set; } = new MockWixStateProvider();

		public MockElasticsearchEnvironmentStateProvider ElasticsearchState { get; private set; } = new MockElasticsearchEnvironmentStateProvider();

		public MockJavaEnvironmentStateProvider JavaState { get; private set; } = new MockJavaEnvironmentStateProvider();

		public NoopServiceStateProvider ServiceState { get; private set; } = new NoopServiceStateProvider();

		public NoopPluginStateProvider PluginState { get; private set; } = new NoopPluginStateProvider();

		public NoopSession SessionState { get; private set; } = new NoopSession();

		private List<string> IntermediateArguments = new List<string>();
		public string[] Arguments => IntermediateArguments.ToArray();
	}
}