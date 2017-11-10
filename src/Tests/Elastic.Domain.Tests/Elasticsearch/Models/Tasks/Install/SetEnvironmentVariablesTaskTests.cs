
using System.IO;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install
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
					t.EsState.UnsetOldConfigVariableWasCalled.Should().BeTrue();
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
					t.EsState.LastSetEsHome.Should().Be(Path.Combine(EsHome, TestSetupStateProvider.DefaultTestVersion));
					t.EsState.LastSetEsConfig.Should().Be(EsConfig);
					t.EsState.UnsetOldConfigVariableWasCalled.Should().BeTrue();
				}
			);
		
		[Fact] void Installing5xSetsNewVariable() => WithValidPreflightChecks(s=>s.Wix(currentVersion: "5.0.0", existingVersion: null))
			.AssertTask(
				(m, s, fs) => new SetEnvironmentVariablesTask(m, s, fs),
				(m, t) => 
				{
					t.EsState.NewConfigDirectoryMachineVariable.Should().Be(m.LocationsModel.ConfigDirectory);
					t.EsState.OldConfigDirectoryMachineVariable.Should().Be(null);
					
					t.EsState.OldConfigDirectoryMachineVariableCopy.Should().Be(null);
					t.EsState.UnsetOldConfigVariableWasCalled.Should().BeTrue();
				}
			);
		
		[Fact] void Installing6xSetsNewVariable() => WithValidPreflightChecks(s=>s.Wix(currentVersion: "6.0.0", existingVersion: null))
			.AssertTask(
				(m, s, fs) => new SetEnvironmentVariablesTask(m, s, fs),
				(m, t) => 
				{
					t.EsState.NewConfigDirectoryMachineVariable.Should().Be(m.LocationsModel.ConfigDirectory);
					t.EsState.OldConfigDirectoryMachineVariable.Should().Be(null);
					t.EsState.OldConfigDirectoryMachineVariableCopy.Should().Be(null);
					t.EsState.UnsetOldConfigVariableWasCalled.Should().BeTrue();
				}
			);


	}
}
