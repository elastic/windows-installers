using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install.EditElasticsearchYaml
{
	public class EditElasticsearchYamlXPackModelTaskTests : InstallationModelTestBase
	{
		[Fact] void WritesExpectedDefauts() => WithValidPreflightChecks(s => s
			.Elasticsearch(es => es
				.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
			)
			.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(@""));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yaml);
					yamlContents.Should().NotBeEmpty().And.NotBe("cluster.name: x");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.XPackLicenseSelfGeneratedType.Should().BeNull();
					s.XPackSecurityEnabled.Should().BeNull();
				}
			);

		[Fact] void SelectingXPackPluginDefaultsToWritingLicenseType() => WithValidPreflightChecks(s => s
			.Elasticsearch(es => es
				.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
			)
			.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(@""));
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yaml);
					yamlContents.Should().NotBeEmpty().And.NotBe("cluster.name: x");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.XPackLicenseSelfGeneratedType.Should().Be(nameof(XPackLicenseMode.Basic).ToLowerInvariant());
					s.XPackSecurityEnabled.Should().BeNull();
				}
			);
		
		[Fact] void SelectingTrialDoesNotWriteSecurityEnabled() => WithValidPreflightChecks(s => s
			.Elasticsearch(es => es
				.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
			)
			.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(@""));
					return fs;
				})
			)
			.OnStep(m => m.XPackModel, s => s.XPackLicense = XPackLicenseMode.Trial)
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yaml);
					yamlContents.Should().NotBeEmpty().And.NotBe("cluster.name: x");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.XPackLicenseSelfGeneratedType.Should().Be(nameof(XPackLicenseMode.Trial).ToLowerInvariant());
					s.XPackSecurityEnabled.Should().BeNull();
				}
			);
		
		[Fact] void DeselectingSecurityWritesFalseToYamlFile() => WithValidPreflightChecks(s => s
			.Elasticsearch(es => es
				.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
			)
			.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(@""));
					return fs;
				})
			)
			.OnStep(m => m.XPackModel, s =>
			{
				s.XPackLicense = XPackLicenseMode.Trial;
				s.XPackSecurityEnabled = false;
			})
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yaml);
					yamlContents.Should().NotBeEmpty().And.NotBe("cluster.name: x");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.XPackLicenseSelfGeneratedType.Should().Be(nameof(XPackLicenseMode.Trial).ToLowerInvariant());
					s.XPackSecurityEnabled.Should().BeFalse();
				}
			);
		
		[Fact] void XPackSettingsAlreadyInPlaceAreNotOverwritten() => WithValidPreflightChecks(s => s
			.Elasticsearch(es => es
				.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
			)
			.FileSystem(fs =>
				{
					var yaml = $@"bootstrap.memory_lock: true
xpack.security.enabled: false
xpack.license.self_generated.type: trial
xpack.random_setting: something
";
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), yaml);
					return fs;
				})
			)
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.MemoryLock.Should().BeTrue();
					s.XPackLicenseSelfGeneratedType.Should().Be("trial");
					s.XPackSecurityEnabled.Should().BeFalse();
					//unknown x-pack setting is preserved
					s.Keys.Where(k => k.StartsWith("xpack")).Should().HaveCount(1);
				}
			);

	}
}
