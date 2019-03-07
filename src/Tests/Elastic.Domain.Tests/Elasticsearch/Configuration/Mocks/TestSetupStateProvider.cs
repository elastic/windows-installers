using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Semver;
using Elastic.Installer.Domain.Tests.Elasticsearch.Models;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{

	public class TestSetupStateProvider
	{
		public static readonly string DefaultTestVersion = "7.0.0";

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
			this.ServiceState = new NoopServiceStateProvider { SeesService = true };
			return this;
		}

		public TestSetupStateProvider PreviouslyInstalledPlugins(params string[] previouslyInstalled)
		{
			this.PluginState = new NoopPluginStateProvider(previouslyInstalled);
			return this;
		}

		public TestSetupStateProvider Wix(string current, string upgradeFrom = null, string previousVersionInstallDir = null)
		{
			this.WixState = new MockWixStateProvider { CurrentVersion = current };
			if (string.IsNullOrWhiteSpace(upgradeFrom)) 
				return this.SessionFromWix();
			this.WixState.UpgradeFromVersion = upgradeFrom;

			if (this.ElasticsearchState == null)
				this.ElasticsearchState = new MockElasticsearchEnvironmentStateProvider();

			if (this.ElasticsearchState.LastSetEsHome == null)
				this.ElasticsearchState.EsHomeMachineVariable(previousVersionInstallDir ?? $@"C:\Elasticsearch\{upgradeFrom}");
			return this.SessionFromWix();
		}

		public TestSetupStateProvider Wix(bool alreadyInstalled, string existingInstallDir = null, bool installationInProgress = false)
		{
			this.WixState = new MockWixStateProvider() { CurrentVersion = DefaultTestVersion, InstallationInProgress = installationInProgress};
			if (!alreadyInstalled) return this.SessionFromWix();
			this.WixState.UpgradeFromVersion = DefaultTestVersion;

			if (this.ElasticsearchState == null)
				this.ElasticsearchState = new MockElasticsearchEnvironmentStateProvider();

			if (this.ElasticsearchState.LastSetEsHome == null)
				this.ElasticsearchState.EsHomeMachineVariable(existingInstallDir ?? $@"C:\Elasticsearch\{DefaultTestVersion}");
			return this.SessionFromWix();
		}
		private TestSetupStateProvider SessionFromWix()
		{
			var sessionVariables = new Dictionary<string, string> {{"CurrentVersion", DefaultTestVersion}};
			var installed = this.WixState.UpgradeFromVersion != null;
			
			if (this.SessionState == null)
			{
				this.SessionState = new NoopSession(nameof(NoopSession.Elasticsearch), sessionVariables)
				{
					IsInstalled = installed,
					IsInstalling = true
				};
			}
			else
			{
				this.SessionState.IsInstalled = installed;
				this.SessionState.IsInstalling = true;
				foreach (var sessionVariable in sessionVariables)
					this.SessionState.Set(sessionVariable.Key, sessionVariable.Value);
			}
		
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

		public TestSetupStateProvider Session(bool uninstalling = true, bool rollback = false, Dictionary<string, string> sessionVariables = null)
		{
			if (this.SessionState == null)
			{
				this.SessionState = new NoopSession(nameof(NoopSession.Elasticsearch), sessionVariables)
				{
					IsUninstalling = uninstalling,
					IsRollback = rollback,
				};
			}
			else
			{
				this.SessionState.IsUninstalling = uninstalling;
				this.SessionState.IsRollback = rollback;
				if (sessionVariables != null)
				{
					foreach (var sessionVariable in sessionVariables)
						this.SessionState.Set(sessionVariable.Key, sessionVariable.Value);					
				}
			}
		
			return this;
		}

		public MockFileSystem FileSystemState { get; private set; } = InstallationModelTester.CreateMockFileSystem();

		public MockWixStateProvider WixState { get; private set; } = new MockWixStateProvider();

		public MockElasticsearchEnvironmentStateProvider ElasticsearchState { get; private set; } = new MockElasticsearchEnvironmentStateProvider();

		public MockJavaEnvironmentStateProvider JavaState { get; private set; } = new MockJavaEnvironmentStateProvider();

		public NoopServiceStateProvider ServiceState { get; private set; } = new NoopServiceStateProvider();

		public NoopPluginStateProvider PluginState { get; private set; } = new NoopPluginStateProvider();

		public NoopSession SessionState { get; private set; }

		private List<string> IntermediateArguments = new List<string>();
		public string[] Arguments => IntermediateArguments.ToArray();
	}
}
