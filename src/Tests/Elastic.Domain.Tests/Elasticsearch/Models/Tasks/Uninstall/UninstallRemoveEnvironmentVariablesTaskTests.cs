using Elastic.InstallerHosts.Elasticsearch.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Uninstall
{
	public class UninstallRemoveEnvironmentVariablesTaskTests : InstallationModelTestBase
	{
		[Fact] void UninstallRemovesEnvironmentVariables() => WithValidPreflightChecks(s=>s
				.Session(uninstalling: true)
			)
			.AssertTask(
				(m, s, fs) =>
				{
					m.ElasticsearchEnvironmentConfiguration.SetEsConfigEnvironmentVariable("config");
					m.ElasticsearchEnvironmentConfiguration.SetEsHomeEnvironmentVariable("home");
					return new RemoveEnvironmentVariablesTask(m, s, fs);
				},
				(m, t) =>
				{
					var env = m.ElasticsearchEnvironmentConfiguration.StateProvider; 
					env.HomeDirectoryMachineVariable.Should().BeNullOrWhiteSpace();
					env.ConfigDirectoryMachineVariable.Should().BeNullOrWhiteSpace();
				}
			);
		
	}
}

