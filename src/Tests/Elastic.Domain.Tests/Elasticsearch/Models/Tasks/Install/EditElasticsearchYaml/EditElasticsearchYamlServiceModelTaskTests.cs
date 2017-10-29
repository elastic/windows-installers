using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install.EditElasticsearchYaml
{
	public class EditElasticsearchYamlServiceModelTaskTests : InstallationModelTestBase
	{
		[Fact] void DefaultMaxLocalStorageNodesNotService() => WithValidPreflightChecks(s => s
			.Elasticsearch(es => es
				.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
			)
			.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(@""));
					return fs;
				})
			)
			.OnStep(m => m.ServiceModel, s => s.InstallAsService = false)
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
					//because we do not install as service
					s.MaxLocalStorageNodes.Should().NotHaveValue();
				}
			);

		[Fact] void MaxLocalStorageNodesShouldBeSetWhenInstallingAsService() => WithValidPreflightChecks(s => s
				.Elasticsearch(es => es
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(@""));
					return fs;
				})
			)
			.OnStep(m => m.ServiceModel, s => s.InstallAsService = true)
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
					//because we do not install as service
					s.MaxLocalStorageNodes.Should().Be(1);
				});

	}
}
