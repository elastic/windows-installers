using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Uninstall;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Uninstall
{
	public class UninstallDeleteDirectoriesTaskTests : InstallationModelTestBase
	{
		[Fact] void RemoveDirectoriesOnUninstall() =>
			DefaultValidModelForTasks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.DataDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.LogsDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				return new UninstallDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(m.LocationsModel.InstallDir).Should().BeFalse("{0}", m.LocationsModel.InstallDir);
				var session = m.Session as NoopSession;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeTrue("{0} {1}", m.LocationsModel.DataDirectory, session);
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeTrue("{0}", m.LocationsModel.ConfigDirectory);
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeTrue("{0}", m.LocationsModel.LogsDirectory);
			}
		);
		
		[Fact] void RemovesParentInstallationFoldersWhenEmpty() =>
			DefaultValidModelForTasks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				fs.Directory.CreateDirectory(LocationsModel.DefaultProductInstallationDirectory);
				return new UninstallDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(LocationsModel.DefaultProductInstallationDirectory).Should()
					.BeFalse("{0}", LocationsModel.DefaultProductInstallationDirectory);
				fs.Directory.Exists(LocationsModel.DefaultCompanyInstallationDirectory).Should()
					.BeFalse("{0}", LocationsModel.DefaultCompanyInstallationDirectory);
			}
		);

		[Fact] void LeavesParentFolderAloneIfOtherProductIsInstalled() =>
			DefaultValidModelForTasks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				fs.Directory.CreateDirectory(LocationsModel.DefaultProductInstallationDirectory);
				fs.Directory.CreateDirectory(fs.Path.Combine(LocationsModel.DefaultCompanyInstallationDirectory, "product2"));
				return new UninstallDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(LocationsModel.DefaultProductInstallationDirectory).Should()
					.BeFalse("{0}", LocationsModel.DefaultProductInstallationDirectory);
				fs.Directory.Exists(LocationsModel.DefaultCompanyInstallationDirectory).Should()
					.BeTrue("{0}", LocationsModel.DefaultCompanyInstallationDirectory);
			}
		);
		
		[Fact] void LeavesCompanyDataDirectoryAloneIfOtherProductUsesIt() =>
			DefaultValidModelForTasks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				fs.Directory.CreateDirectory(LocationsModel.DefaultProductDataDirectory);
				fs.Directory.CreateDirectory(fs.Path.Combine(LocationsModel.DefaultCompanyDataDirectory, "product2"));
				return new UninstallDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(LocationsModel.DefaultProductDataDirectory).Should()
					.BeTrue("{0}", LocationsModel.DefaultProductDataDirectory);
				fs.Directory.Exists(LocationsModel.DefaultCompanyDataDirectory).Should()
					.BeTrue("{0}", LocationsModel.DefaultCompanyDataDirectory);
			}
		);

	}
}
