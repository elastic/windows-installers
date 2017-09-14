using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Rollback
{
	public class RollbackDeleteDirectoriesTaskTests : InstallationModelTestBase
	{
		[Fact] void RollbackNewInstallationRemovesDirectories() =>
			WithValidPreflightChecks(s => s
				.Session(rollback: true, uninstalling: false)
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
				var session = m.Session as NoopSession;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should()
					.BeFalse("{0} {1}", m.LocationsModel.DataDirectory, session)
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeFalse("{0}", m.LocationsModel.ConfigDirectory);
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeFalse("{0}", m.LocationsModel.LogsDirectory);
				fs.Directory.Exists(m.LocationsModel.InstallDir).Should().BeFalse("{0}", m.LocationsModel.InstallDir);
			}
		);

		[Fact] void RollbackToPreviousInstallationDoesNotRemoveDirectories() =>
			WithValidPreflightChecks(s => s
				.Wix(currentVersion: "5.6.0", existingVersion: "5.5.0")
				.Session(rollback: true, uninstalling: false)
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
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeTrue("{0}", m.LocationsModel.DataDirectory);
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeTrue("{0}", m.LocationsModel.ConfigDirectory);
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeTrue("{0}", m.LocationsModel.LogsDirectory);
				fs.Directory.Exists(m.LocationsModel.InstallDir).Should().BeTrue("{0}", m.LocationsModel.InstallDir);
			}
		);
	}
}
