using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install
{
	public class EditJvmOptionsTaskTests : InstallationModelTestBase
	{
		[Fact]
		void PicksUpExistingConfiguration() => DefaultValidModelForTasks(s => s
				.Elasticsearch(e => e
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "jvm.options"), new MockFileData(@"-Xmx8000m
-SomeOtherJvmOption"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditJvmOptionsTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var jvmOptionsFile = Path.Combine(dir, "jvm.options");
					var jvmOptionsFileContents = t.FileSystem.File.ReadAllText(jvmOptionsFile);
					jvmOptionsFileContents.Should()
						.NotBeEmpty()
						.And.Contain("-Xmx8000m")
						.And.Contain("-SomeOtherJvmOption")
						;
					var jvmOptions = LocalJvmOptionsConfiguration.FromFolder(dir, t.FileSystem);
					jvmOptions.ConfiguredHeapSize.Should().Be((ulong) 8000);
				}
			);

		[Fact]
		void WritesExpectedDefaults() => DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "jvm.options"), new MockFileData(""));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditJvmOptionsTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var jvmOptionsFile = Path.Combine(dir, "jvm.options");
					var jvmOptionsFileContents = t.FileSystem.File.ReadAllText(jvmOptionsFile);
					jvmOptionsFileContents.Should()
						.NotBeEmpty()
						.And.Contain($"-Xmx{ConfigurationModel.DefaultHeapSize}m")
						.And.Contain($"-Xms{ConfigurationModel.DefaultHeapSize}m");
					var jvmOptions = LocalJvmOptionsConfiguration.FromFolder(dir, t.FileSystem);
					jvmOptions.ConfiguredHeapSize.Should().Be(ConfigurationModel.DefaultHeapSize);
				}
			);

		[Fact]
		void WritesConfiguredMemory() => DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "jvm.options"), new MockFileData(""));
					return fs;
				})
			)
			.OnStep(m => m.ConfigurationModel, s => s.SelectedMemory = 1024)
			.AssertTask(
				(m, s, fs) => new EditJvmOptionsTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var jvmOptionsFile = Path.Combine(dir, "jvm.options");
					var jvmOptionsFileContents = t.FileSystem.File.ReadAllText(jvmOptionsFile);
					jvmOptionsFileContents.Should()
						.NotBeEmpty()
						.And.Contain($"-Xmx1024m")
						.And.Contain($"-Xms1024m");
					var jvmOptions = LocalJvmOptionsConfiguration.FromFolder(dir, t.FileSystem);
					jvmOptions.ConfiguredHeapSize.Should().Be((ulong) 1024);
				}
			);

		[Fact]
		void WritesConfiguredGCLogPath() => DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "jvm.options"),
						new MockFileData(@"9-:-Xlog:gc*,gc+age=trace,safepoint:file=logs/gc.log:utctime,pid,tags:filecount=32,filesize=64m"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditJvmOptionsTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var jvmOptionsFile = Path.Combine(dir, "jvm.options");
					var jvmOptionsFileContents = t.FileSystem.File.ReadAllText(jvmOptionsFile);
					jvmOptionsFileContents.Should()
						.NotBeEmpty()
						.And.Contain(@"9-:-Xlog:gc*,gc+age=trace,safepoint:file=""${ES_PATH_LOGS}/gc.log"":utctime,pid,tags:filecount=32,filesize=64m");
				}
			);

		[Fact]
		void DoesNotOverwriteConfiguredGCLogPathIfAlreadyExists() => DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "jvm.options"),
						new MockFileData(@"9-:-Xlog:gc*,gc+age=trace,safepoint:file=""${ES_PATH_LOGS}/gc.log"":utctime,pid,tags:filecount=32,filesize=64m"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditJvmOptionsTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var jvmOptionsFile = Path.Combine(dir, "jvm.options");
					var jvmOptionsFileContents = t.FileSystem.File.ReadAllText(jvmOptionsFile);
					jvmOptionsFileContents.Should()
						.NotBeEmpty()
						.And.Contain(@"9-:-Xlog:gc*,gc+age=trace,safepoint:file=""${ES_PATH_LOGS}/gc.log"":utctime,pid,tags:filecount=32,filesize=64m");
				}
			);

		[Fact]
		void DoesNotOverwriteConfiguredGCLogPathIfCustom() => DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "jvm.options"),
						new MockFileData(@"9-:-Xlog:gc*,gc+age=trace,safepoint:file=/ProgramData/Elastic/gc.log:utctime,pid,tags:filecount=32,filesize=64m"));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditJvmOptionsTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var jvmOptionsFile = Path.Combine(dir, "jvm.options");
					var jvmOptionsFileContents = t.FileSystem.File.ReadAllText(jvmOptionsFile);
					jvmOptionsFileContents.Should()
						.NotBeEmpty()
						.And.Contain(@"9-:-Xlog:gc*,gc+age=trace,safepoint:file=/ProgramData/Elastic/gc.log:utctime,pid,tags:filecount=32,filesize=64m");
				}
			);
	}
}