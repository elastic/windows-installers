using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Rollback;
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
				return new RollbackDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				var session = m.Session as NoopSession;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should()
					.BeFalse("{0} {1} {2}", m.LocationsModel.DataDirectory, session, DumpFileSystem(fs));
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeFalse("{0}", m.LocationsModel.ConfigDirectory);
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeFalse("{0}", m.LocationsModel.LogsDirectory);
			}
		);

		public string DumpFileSystem(MockFileSystem fs) =>
			fs.AllPaths.Aggregate(new StringBuilder().AppendLine("FileSystem:"), (sb, s) => sb.AppendLine($" {s}"), sb => sb.ToString());

		[Fact] void RollbackToPreviousInstallationDoesNotRemoveDirectories() =>
			WithValidPreflightChecks(s => s
				.Wix(installerVersion: "5.6.0", previousVersion: "5.5.0")
				.Session(rollback: true, uninstalling: false)
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.DataDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.LogsDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				return new RollbackDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeFalse("{0}", m.LocationsModel.DataDirectory);
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeFalse("{0}", m.LocationsModel.ConfigDirectory);
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeFalse("{0}", m.LocationsModel.LogsDirectory);
			}
		);
		
		[Fact] void RollbackToPreviousInstallationDoesNotRemoveDirectoriesWhenSessionVariablesAreSet() =>
			WithValidPreflightChecks(s => s
				.Wix(installerVersion: "5.6.0", previousVersion: "5.5.0")
				.Session(rollback: true, uninstalling: false, sessionVariables: new Dictionary<string, string>
					{
						{ "ConfigDirectoryExists", "True"},
						{ "LogsDirectoryExists", "True"},
						{ "DataDirectoryExists", "True"}
					})
			)
			.AssertTask((m, s, fs) =>
			{
				fs.Directory.CreateDirectory(m.LocationsModel.DataDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.ConfigDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.LogsDirectory);
				fs.Directory.CreateDirectory(m.LocationsModel.InstallDir);
				return new RollbackDirectoriesTask(m, s, fs);
			},
			(m, t) =>
			{
				var fs = t.FileSystem;
				fs.Directory.Exists(m.LocationsModel.DataDirectory).Should().BeTrue("{0}", m.LocationsModel.DataDirectory);
				fs.Directory.Exists(m.LocationsModel.ConfigDirectory).Should().BeTrue("{0}", m.LocationsModel.ConfigDirectory);
				fs.Directory.Exists(m.LocationsModel.LogsDirectory).Should().BeTrue("{0}", m.LocationsModel.LogsDirectory);
			}
		);
	}
}
