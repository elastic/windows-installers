using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Runtime.CompilerServices;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Commit;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Commit
{
	public class CleanUpInstallTaskTests : InstallationModelTestBase
	{
		[Fact] void CleansProductInstallTempDirectory() => WithValidPreflightChecks()
			.AssertTask(
				(m, s, fs) =>
				{
					var tempDir = m.TempDirectoryConfiguration.TempProductInstallationDirectory;
					var untrackedDir = Path.Combine(tempDir, "another-folder");
					var untrackedFile = Path.Combine(untrackedDir, "some-file.yml");
					fs.AddDirectory(untrackedDir);
					fs.AddFile(untrackedFile, MockFileData.NullObject);
					fs.Directory.Exists(untrackedDir).Should().BeTrue();
					fs.File.Exists(untrackedFile).Should().BeTrue();
					return new CleanupInstallTask(m, s, fs);
				},
				(m, t) =>
				{
					var tempDir = m.TempDirectoryConfiguration.TempProductInstallationDirectory;
					var untrackedDir = Path.Combine(tempDir, "another-folder");
					var untrackedFile = Path.Combine(untrackedDir, "some-file.yml");
					// Cleanup cleans the entire temporary product installation directory
					var fs = t.FileSystem;
					fs.Directory.Exists(untrackedDir).Should().BeFalse();
					fs.File.Exists(untrackedFile).Should().BeFalse();
				}
			);
	}
}

