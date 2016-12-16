using System.IO;
using System.IO.Abstractions.TestingHelpers;
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
				.HomeDirectoryEnvironmentVariable(_customHome)
			))
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir.Should().Be(_customHome);
				step.ConfigureLocations.Should().BeTrue();
			});

		[Fact] void ConfigDirectoryReflectsES_CONFIG() => WithValidPreflightChecks(s => s
			.Elasticsearch(e => e
				.ConfigDirectoryEnvironmentVariable(_customConfig)
			))
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigDirectory.Should().Be(_customConfig);
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
			});

		[Fact] void DataDirectoryIsReadFromElasticsearchYaml() => WithValidPreflightChecks(s => s
			.Elasticsearch(e => e
				.HomeDirectoryEnvironmentVariable(_customHome)
				.ConfigDirectoryEnvironmentVariable(_customConfig)
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
				.HomeDirectoryEnvironmentVariable(_customHome)
				.ConfigDirectoryEnvironmentVariable(_customConfig)
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
