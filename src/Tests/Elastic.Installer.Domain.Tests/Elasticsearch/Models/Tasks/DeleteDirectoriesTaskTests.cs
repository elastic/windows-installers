using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks
{
	public class DeleteDirectoriesTaskTests : InstallationModelTestBase
	{
		[Fact(Skip = "ignore for now")]
		void DeleteDefaultDirectoriesOnCleanInstallRollback() =>
			WithValidPreflightChecks(s => s
				.Session(false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.DataDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.LogsDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				return new DeleteDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeFalse();
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeFalse();
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeFalse();
				fs.Directory.Exists(m.LocationsModel.InstallDir).Should().BeFalse();
			}
		);

		[Fact(Skip = "ignore for now")]
		void RemoveDirectoriesOnUninstall() =>
			WithValidPreflightChecks(s => s
				.Wix("5.0.1", "5.0.0")
				.Session(true)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.DataDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.LogsDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				return new DeleteDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeFalse();
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeFalse();
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeFalse();
				fs.Directory.Exists(m.LocationsModel.InstallDir).Should().BeFalse();
			}
		);

		[Fact]
		void DoesNotDeleteDirectoriesOnPreviousInstallRollback() => WithValidPreflightChecks(s => s
				.Wix("5.0.1", "5.0.0")
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.DataDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.LogsDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				return new DeleteDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeTrue();
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeTrue();
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeTrue();
				fs.Directory.Exists(m.LocationsModel.InstallDir).Should().BeTrue();
			}
		);
	}
}
