using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Uninstall
{
	public class UninstallDeleteDirectoriesTaskTests : InstallationModelTestBase
	{
		[Fact] void RemoveDirectoriesOnUninstall() =>
			WithValidPreflightChecks(s => s
				.Session(uninstalling: true, rollback: false)
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
		
		[Fact] void RemovesParentInstallationFoldersWhenEmpty() =>
			WithValidPreflightChecks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				fs.Directory.CreateDirectory(LocationsModel.DefaultProductInstallationDirectory);
				return new DeleteDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(LocationsModel.DefaultProductInstallationDirectory).Should().BeFalse();
				fs.Directory.Exists(LocationsModel.DefaultCompanyInstallationDirectory).Should().BeFalse();
			}
		);
		[Fact] void LeavesParentFolderAloneIfOtherProductIsInstalled() =>
			WithValidPreflightChecks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				fs.Directory.CreateDirectory(LocationsModel.DefaultProductInstallationDirectory);
				fs.Directory.CreateDirectory(fs.Path.Combine(LocationsModel.DefaultCompanyInstallationDirectory, "product2"));
				return new DeleteDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(LocationsModel.DefaultProductInstallationDirectory).Should().BeFalse();
				fs.Directory.Exists(LocationsModel.DefaultCompanyInstallationDirectory).Should().BeTrue();
			}
		);
		
		[Fact] void RemovesDefaultDataFoldersWhenEmpty() =>
			WithValidPreflightChecks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				fs.Directory.CreateDirectory(LocationsModel.DefaultProductDataDirectory);
				return new DeleteDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(LocationsModel.DefaultProductDataDirectory).Should().BeFalse();
				fs.Directory.Exists(LocationsModel.DefaultCompanyDataDirectory).Should().BeFalse();
			}
		);
		
		[Fact] void LeavesCompanyDataDirectoryAloneIfOtherProductUsesIt() =>
			WithValidPreflightChecks(s => s
				.Session(uninstalling: true, rollback: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				fs.Directory.CreateDirectory(LocationsModel.DefaultProductDataDirectory);
				fs.Directory.CreateDirectory(fs.Path.Combine(LocationsModel.DefaultCompanyDataDirectory, "product2"));
				return new DeleteDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(LocationsModel.DefaultProductDataDirectory).Should().BeFalse();
				fs.Directory.Exists(LocationsModel.DefaultCompanyDataDirectory).Should().BeTrue();
			}
		);

	}
}
