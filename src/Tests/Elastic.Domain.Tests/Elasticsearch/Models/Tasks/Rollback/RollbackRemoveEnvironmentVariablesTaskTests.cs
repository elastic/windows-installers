using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Commit;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Rollback
{
	public class RollbackRemoveEnvironmentVariablesTaskTests : InstallationModelTestBase
	{
		[Fact] void RollbackToPreviousInstall() => WithValidPreflightChecks(s=>s
				.Wix(currentVersion: "5.5.0", existingVersion: "5.4.0")
				.Session(rollback: true, uninstalling: false)
			)
			.ExecuteTask((m,s,fs) => new SetEnvironmentVariablesTask(m, s, fs))
			.AssertTask(
				(m, s, fs) =>
				{
					var env = m.ElasticsearchEnvironmentConfiguration.StateProvider; 
					env.HomeDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
					env.ConfigDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
					env.NewConfigDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
					return new RemoveEnvironmentVariablesTask(m, s, fs);
				},
				(m, t) =>
				{
					var env = m.ElasticsearchEnvironmentConfiguration.StateProvider; 
					env.HomeDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
					env.ConfigDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
					env.NewConfigDirectoryMachineVariable.Should().NotBeNullOrWhiteSpace();
				}
			);
		[Fact] void RollbackNewInstallation() => WithValidPreflightChecks(s=>s
				.Wix(currentVersion: "5.5.0", existingVersion: null)
				.Session(rollback: true, uninstalling: false)
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
					env.NewConfigDirectoryMachineVariable.Should().BeNullOrWhiteSpace();
					env.ConfigDirectoryMachineVariable.Should().BeNullOrWhiteSpace();
				}
			);
		
	}
}

