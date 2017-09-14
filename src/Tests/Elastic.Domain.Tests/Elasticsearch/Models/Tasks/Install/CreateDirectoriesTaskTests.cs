using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install
{
	public class CreateDirectoriesTaskTests : InstallationModelTestBase
	{
		private readonly string EsConfig = @"c:\es-config-folder";
		private readonly string EsData = @"c:\es-data";
		private readonly string EsLogs = @"c:\es-logs";

		[Fact] void CreatesDefaultDirectories() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) =>
				{
					fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeFalse();
					fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeFalse();
					fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeFalse();
					return new CreateDirectoriesTask(m, s, fs);
				},
				(m, t) =>
				{
					var fs = t.FileSystem;
					fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeTrue();
					fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeTrue();
					fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeTrue();
				}
			);

		[Fact] void CreatesUserDirectories() => WithValidPreflightChecks(s => s
				.SetupArgument(nameof(LocationsModel.DataDirectory), EsData)
				.SetupArgument(nameof(LocationsModel.ConfigDirectory), EsConfig)
				.SetupArgument(nameof(LocationsModel.LogsDirectory), EsLogs)
			)
			.AssertTask(
				(m, s, fs) =>
				{
					fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeFalse();
					fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeFalse();
					fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeFalse();
					return new CreateDirectoriesTask(m, s, fs);
				},
				(m, t) =>
				{
					m.LocationsModel.DataDirectory.Should().Be(EsData);
					m.LocationsModel.ConfigDirectory.Should().Be(EsConfig);
					m.LocationsModel.LogsDirectory.Should().Be(EsLogs);
					var fs = t.FileSystem;
					fs.Directory.Exists(EsData).Should().BeTrue();
					fs.Directory.Exists(EsConfig).Should().BeTrue();
					fs.Directory.Exists(EsLogs).Should().BeTrue();
				}
			);

		[Fact] void ExisitingDirectoriesAreReused() => WithValidPreflightChecks(s => s
				.SetupArgument(nameof(LocationsModel.DataDirectory), EsData)
				.SetupArgument(nameof(LocationsModel.ConfigDirectory), EsConfig)
				.SetupArgument(nameof(LocationsModel.LogsDirectory), EsLogs)
			)
			.AssertTask(
				(m, s, fs) =>
				{
					fs.Directory.CreateDirectory(EsData);
					fs.Directory.CreateDirectory(EsConfig);
					fs.Directory.CreateDirectory(EsLogs);
					return new CreateDirectoriesTask(m, s, fs);
				},
				(m, t) =>
				{
					m.LocationsModel.DataDirectory.Should().Be(EsData);
					m.LocationsModel.ConfigDirectory.Should().Be(EsConfig);
					m.LocationsModel.LogsDirectory.Should().Be(EsLogs);
					var fs = t.FileSystem;
					fs.Directory.Exists(EsData).Should().BeTrue();
					fs.Directory.Exists(EsConfig).Should().BeTrue();
					fs.Directory.Exists(EsLogs).Should().BeTrue();
				}
			);

		[Fact] void DefaultConfigContentsCopied() => WithValidPreflightChecks(s => s
				.SetupArgument(nameof(LocationsModel.DataDirectory), EsData)
				.SetupArgument(nameof(LocationsModel.ConfigDirectory), EsConfig)
				.SetupArgument(nameof(LocationsModel.LogsDirectory), EsLogs)
				.FileSystem(fs => 
				{
					fs.AddDirectory(Path.Combine(LocationsModel.DefaultProductInstallationDirectory, "config"));
					var conf = Path.Combine(LocationsModel.DefaultProductInstallationDirectory, "config", "random-file.yml");
					fs.AddFile(conf, new MockFileData("node.name: x"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) =>
				{
					return new CreateDirectoriesTask(m, s, fs);
				},
				(m, t) =>
				{
					m.LocationsModel.ConfigDirectory.Should().Be(EsConfig);
					var fs = t.FileSystem;
					fs.Directory.Exists(EsConfig).Should().BeTrue();
					var original = Path.Combine(LocationsModel.DefaultProductInstallationDirectory, "config", "elasticsearch.yml");
					var moved = Path.Combine(EsConfig, "random-file.yml");
					fs.File.Exists(moved).Should().BeTrue();
					fs.File.Exists(original).Should().BeFalse();
				}
			);

	}
}
