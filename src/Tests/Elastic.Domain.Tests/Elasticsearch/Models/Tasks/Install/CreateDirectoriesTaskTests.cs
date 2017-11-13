using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
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
		
		[Fact(Skip = "waiting for https://github.com/tathamoddie/System.IO.Abstractions/pull/259 to be merged")]
		void CreatesDefaultDirectories() => WithValidPreflightChecks()
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

		[Fact(Skip = "waiting for https://github.com/tathamoddie/System.IO.Abstractions/pull/259 to be merged")]
		void CreatesUserDirectories() => WithValidPreflightChecks(s => s
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

		[Fact(Skip = "waiting for https://github.com/tathamoddie/System.IO.Abstractions/pull/259 to be merged")]
		void ExisitingDirectoriesAreReused() => WithValidPreflightChecks(s => s
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

		[Fact(Skip = "waiting for https://github.com/tathamoddie/System.IO.Abstractions/pull/259 to be merged")]
		void DefaultConfigContentsCopied() => WithValidPreflightChecks(s => s
				.SetupArgument(nameof(LocationsModel.DataDirectory), EsData)
				.SetupArgument(nameof(LocationsModel.ConfigDirectory), EsConfig)
				.SetupArgument(nameof(LocationsModel.LogsDirectory), EsLogs)
				.FileSystem(fs => 
				{
					var configDir = Path.Combine(VersionSpecificInstallDirectory, "config");
					var configDirSubDir = Path.Combine(configDir, "sub");
					var conf = Path.Combine(configDir, "random-file.yml");
					var subConf = Path.Combine(configDirSubDir, "sub-file.yml");
					fs.AddDirectory(configDir);
					fs.AddDirectory(configDirSubDir);
					fs.AddFile(conf, new MockFileData("node.name: x"));
					fs.AddFile(subConf, new MockFileData("node.name: y"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new CreateDirectoriesTask(m, s, fs),
				(m, t) =>
				{
					m.LocationsModel.ConfigDirectory.Should().Be(EsConfig);
					
					var fs = t.FileSystem;
					var configDir = Path.Combine(VersionSpecificInstallDirectory, "config");
					var configDirSubDir = Path.Combine(configDir, "sub");
					var conf = Path.Combine(configDir, "random-file.yml");
					var subConf = Path.Combine(configDirSubDir, "sub-file.yml");

					fs.Directory.Exists(EsConfig).Should().BeTrue();
					var moved = Path.Combine(EsConfig, "random-file.yml");
					var movedSubDir = Path.Combine(EsConfig, "sub");
					var movedSub = Path.Combine(movedSubDir, "sub-file.yml");
					fs.File.Exists(moved).Should().BeTrue();
					fs.Directory.Exists(movedSubDir).Should().BeTrue();
					fs.File.Exists(movedSub).Should().BeTrue();
					var original = Path.Combine(LocationsModel.DefaultProductInstallationDirectory, "config", "elasticsearch.yml");
					fs.File.Exists(original).Should().BeFalse();
				}
			);

	}
}
