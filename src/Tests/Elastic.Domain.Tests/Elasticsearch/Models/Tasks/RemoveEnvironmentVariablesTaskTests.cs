using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks
{
	public class RemoveEnvironmentVariablesTaskTests : InstallationModelTestBase
	{

		[Fact] void RemoveExpectedEnvironmentVariablesOnCleanUninstall() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) =>
				{
					var task = new RemoveEnvironmentVariablesTask(m, s, fs);
					var state = m.ElasticsearchEnvironmentConfiguration.StateProvider as MockElasticsearchEnvironmentStateProvider;
					state.LastSetEsConfig = "some value";
					state.LastSetEsHome = "some value";
					return task;
				},
				(m, t) => 
				{
					t.EsState.LastSetEsHome.Should().BeNull();
					t.EsState.LastSetEsConfig.Should().BeNull();
				}
			);

		[Fact] void KeepEnvironmentVariablesOnPreviousInstallRollback() => WithValidPreflightChecks(s=>s
				.Wix("5.0.1", "5.0.0")
			)
			.AssertTask(
				(m, s, fs) =>
				{
					var task = new RemoveEnvironmentVariablesTask(m, s, fs);
					var state = m.ElasticsearchEnvironmentConfiguration.StateProvider as MockElasticsearchEnvironmentStateProvider;
					state.LastSetEsConfig = "some previous value";
					state.LastSetEsHome = "some previous value";
					return task;
				},
				(m, t) => 
				{
					t.EsState.LastSetEsHome.Should().Be("some previous value");
					t.EsState.LastSetEsConfig.Should().Be("some previous value");
				}
			);


	}
}
