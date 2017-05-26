using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks
{
	public class SetEnvironmentVariablesTaskTests : InstallationModelTestBase
	{
		private readonly string EsConfig = @"c:\es-config-folder";
		private readonly string EsHome = @"c:\es";

		[Fact] void SetsAllExpectedEnvironmentVariables() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) => new SetEnvironmentVariablesTask(m, s, fs),
				(m, t) => 
				{
					t.EsState.LastSetEsHome.Should().Be(m.LocationsModel.InstallDir);
					t.EsState.LastSetEsConfig.Should().Be(m.LocationsModel.ConfigDirectory);
				}
			);

		[Fact] void SetsConfiguredEnvironmentVariables() => WithValidPreflightChecks(s=>s
				.SetupArgument(nameof(LocationsModel.InstallDir), EsHome)
				.SetupArgument(nameof(LocationsModel.ConfigDirectory), EsConfig)
			)
			.AssertTask(
				(m, s, fs) => new SetEnvironmentVariablesTask(m, s, fs),
				(m, t) => 
				{
					t.EsState.LastSetEsHome.Should().Be(EsHome);
					t.EsState.LastSetEsConfig.Should().Be(EsConfig);
				}
			);


	}
}
