using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Location
{
	public class SeedLocationsTests : InstallationModelTestBase
	{
		private string _customHome = "C:\\Elasticsearch";
		private string _customConfig = "C:\\MyConfigLocation";

		[Fact] void InstallationDirectoryReflectsES_HOME() => WithValidPreflightChecks(s => s
			.Elasticsearch(e => e
				.EsHomeMachineVariable(_customHome)
			))
			.OnStep(m => m.LocationsModel, step =>
			{
				var customHomeVersion = Path.Combine(_customHome, TestSetupStateProvider.DefaultTestVersion);
				step.InstallDir.Should().Be(customHomeVersion);
				step.ConfigureLocations.Should().BeTrue();
			});

		[Fact] void ConfigDirectoryReflectsES_PATH_CONF() => WithValidPreflightChecks(s => s
			.Elasticsearch(e => e
				.EsConfigMachineVariable(_customConfig)
			))
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigDirectory.Should().Be(_customConfig);
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
			});

		[Fact] void DataDirectoryIsReadFromElasticsearchYaml() => WithValidPreflightChecks(s => s
			.Elasticsearch(e => e
				.EsHomeMachineVariable(_customHome)
				.EsConfigMachineVariable(_customConfig)
			)
			.FileSystem(f=>
			{
				f.AddFile(Path.Combine(_customConfig, "elasticsearch.yml"), new MockFileData($@"path.data: {_customHome}\MyDataFolder"));
				return f;
			}))
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
				step.DataDirectory.Should().Be($"{_customHome}\\MyDataFolder");
			});

		[Fact] void LogsDirectoryIsReadFromElasticsearchYaml() => WithValidPreflightChecks(s => s
			.Elasticsearch(e => e
				.EsHomeMachineVariable(_customHome)
				.EsConfigMachineVariable(_customConfig)
			)
			.FileSystem(f=>
			{
				f.AddFile(Path.Combine(_customConfig, "elasticsearch.yml"), new MockFileData($@"path.logs: {_customHome}\MyDataFolder"));
				return f;
			}))
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
				step.LogsDirectory.Should().Be($"{_customHome}\\MyDataFolder");
			});
	}
}
