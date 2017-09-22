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
	public class EditElasticsearchYamlTaskTests : InstallationModelTestBase
	{
		[Fact] void PicksUpExistingConfiguration() => WithValidPreflightChecks(s => s
				.Elasticsearch(e => e
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(fs =>
				{
					fs.AddDirectory(LocationsModel.DefaultConfigDirectory);
					var yaml = Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml");
					fs.AddFile(yaml, new MockFileData(@"cluster.name: x"));
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
					s.ClusterName.Should().Be("x");
					s.NodeName.Should().Be(ConfigurationModel.DefaultNodeName);
				}
			);

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
					s.ClusterName.Should().Be(ConfigurationModel.DefaultClusterName);
					s.NodeName.Should().Be(ConfigurationModel.DefaultNodeName);
					s.MasterNode.Should().Be(ConfigurationModel.DefaultMasterNode);
					s.DataNode.Should().Be(ConfigurationModel.DefaultDataNode);
					s.IngestNode.Should().Be(ConfigurationModel.DefaultIngestNode);
					s.MemoryLock.Should().Be(ConfigurationModel.DefaultMemoryLock);
					s.LogsPath.Should().Be(LocationsModel.DefaultLogsDirectory);
					s.DataPath.Should().Be(LocationsModel.DefaultDataDirectory);
					//because install as service is enabled by default
					s.MaxLocalStorageNodes.Should().Be(1);
					s.NetworkHost.Should().BeNullOrEmpty();
					s.HttpPortString.Should().Be("9200");
					s.TransportTcpPortString.Should().Be("9300");
					s.UnicastHosts.Should().BeNullOrEmpty();
				}
			);

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

		[Fact] void CustomConfigValues() => WithValidPreflightChecks(s => s
			.Elasticsearch(es => es
				.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
			)
			.FileSystem(fs =>
				{
					fs.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(@""));
					return fs;
				})
			)
			.OnStep(m => m.ConfigurationModel, s =>
			{
				s.ClusterName = "x";
				s.NodeName = "x";
				s.MasterNode = false;
				s.DataNode = false;
				s.IngestNode = false;
				s.LockMemory = false;
				s.NetworkHost = "xyz";
				s.HttpPort = 80;
				s.TransportPort = 9300;
				s.UnicastNodes = new ReactiveUI.ReactiveList<string>
				{
					"localhost", "192.2.3.1:9301"
				};
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
					s.ClusterName.Should().Be("x");
					s.NodeName.Should().Be("x");
					s.MasterNode.Should().BeFalse();
					s.IngestNode.Should().BeFalse();
					s.MemoryLock.Should().BeFalse();
					s.NetworkHost.Should().Be("xyz");
					s.HttpPort.Should().Be(80);
					s.HttpPortString.Should().Be("80");
					s.TransportTcpPort.Should().Be(9300);
					s.TransportTcpPortString.Should().Be("9300");
					s.UnicastHosts.Should().BeEquivalentTo(new List<string>
					{
						"localhost", "192.2.3.1:9301"
					});
				}
			);

	}
}
