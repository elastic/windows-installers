using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Location
{
	public class LocationsArgumentsTests : InstallationModelArgumentsTestsBase
	{
		protected readonly string CustomHome = @"C:\\Elasticsearch";
		protected readonly string CustomConfig = @"C:\\Conf";

		[Fact] void ConfigDirectory() => Argument(nameof(LocationsModel.ConfigDirectory), this.CustomConfig, m =>
		{
			m.LocationsModel.ConfigDirectory.Should().Be(this.CustomConfig);
			m.LocationsModel.ConfigureLocations.Should().BeTrue();
			m.LocationsModel.ConfigureAllLocations.Should().BeTrue();
		});

		[Fact] void DataDirectory() => Argument(nameof(LocationsModel.DataDirectory), this.CustomConfig, m =>
		{
			m.LocationsModel.DataDirectory.Should().Be(this.CustomConfig);
		});

		[Fact] void LogsDirectory() => Argument(nameof(LocationsModel.LogsDirectory), this.CustomConfig, m =>
		{
			m.LocationsModel.LogsDirectory.Should().Be(this.CustomConfig);
		});

		[Fact] void InstallDirectory() => Argument(nameof(LocationsModel.InstallDir), this.CustomHome, m =>
		{
			m.LocationsModel.InstallDir.Should().Be(this.CustomHome);
			m.LocationsModel.ConfigureLocations.Should().BeTrue();
		});
	}
}
