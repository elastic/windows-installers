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
		[Fact] void PicksUpExistingConfiguration() => DefaultValidModelForTasks(s => s
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

		[Fact] void WritesExpectedDefauts() => DefaultValidModelForTasks(s => s
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
					s.SeedHosts.Should().BeNullOrEmpty();
				}
			);

		[Fact] void DefaultMaxLocalStorageNodesNotService() => DefaultValidModelForTasks(s => s
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

		[Fact] void CustomConfigValues() => DefaultValidModelForTasks(s => s
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
				s.SeedHosts = new ReactiveUI.ReactiveList<string>
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
					s.SeedHosts.Should().BeEquivalentTo(new List<string>
					{
						"localhost", "192.2.3.1:9301"
					});
				}
			);
		
		[Fact] void WritesSeedHostsFor7() => 
			DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory))
				.FileSystem(fs =>
				{
					var yamlLocation = Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml");
					fs.AddFile(yamlLocation, new MockFileData(@"discovery.zen.ping.unicast.hosts: ['192.2.3.1:9200']"));
					return fs;
				})
			)
			.OnStep(m => m.ConfigurationModel, s =>
			{
				s.ClusterName = "x";
				s.NodeName = "x";
				//we read unicast host from previous install into seed hosts
				s.SeedHosts.Should().NotBeEmpty().And.Contain(h => h.Contains("9200"));
				//simulate a user change
				s.SeedHosts = new ReactiveUI.ReactiveList<string> { "localhost", "192.2.3.1:9201" };
			})
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yaml);
					// validate we are writing the yaml file in 7.0 format
					yamlContents.Should().NotBeEmpty()
						.And.Contain("discovery.seed_hosts")
						.And.Contain("192.2.3.1:9201")
						.And.NotContain("discovery.zen.ping.unicast.hosts")
						.And.NotContain("192.2.3.1:9200");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.ClusterName.Should().Be("x");
					s.NodeName.Should().Be("x");
					s.SeedHosts.Should().BeEquivalentTo(new List<string>
					{
						"localhost", "192.2.3.1:9201"
					});
				}
			);
		
		[Fact] void WritesUnicastHostsFor6() => 
			DefaultValidModelForTasks(s => s
				.Wix(current: "6.6.0")
				.Elasticsearch(es => es.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory))
				.FileSystem(fs =>
				{
					var yamlLocation = Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml");
					fs.AddFile(yamlLocation, new MockFileData(@"discovery.zen.ping.unicast.hosts: ['192.2.3.1:9201']"));
					return fs;
				})
			)
			.OnStep(m => m.ConfigurationModel, s =>
			{
				s.ClusterName = "x";
				s.NodeName = "x";
				//we read unicast host from previous install into seed hosts
				s.SeedHosts.Should().NotBeEmpty().And.Contain(h => h.Contains("9201"));
			})
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yamlPath = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yamlPath);
					// validate we are writing the yaml file in 7.0 format
					yamlContents.Should().NotBeEmpty()
						.And.NotContain("discovery.seed_hosts")
						.And.Contain("192.2.3.1:9201")
						.And.Contain("discovery.zen.ping.unicast.hosts");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var yaml = config.Settings;
					yaml.ClusterName.Should().Be("x");
					yaml.NodeName.Should().Be("x");
					yaml.UnicastHosts.Should().BeEquivalentTo(new List<string> { "192.2.3.1:9201" });
					yaml.SeedHosts.Should().BeNull();
				}
			);
		
		[Fact] void InitialMaster7SetsInitialMasterNodesIfNotSetPrior() => 
			DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory))
				.FileSystem(fs =>
				{
					var yamlLocation = Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml");
					fs.AddFile(yamlLocation, new MockFileData(@"discovery.zen.minimum_master_nodes: 20"));
					return fs;
				})
			)
			.OnStep(m => m.ConfigurationModel, s =>
			{
				s.ClusterName = "x";
				s.NodeName = "nodex";
				s.InitialMaster.Should().BeFalse();
				s.InitialMaster = true;
			})
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yaml);
					// validate we are writing the yaml file in 7.0 format
					yamlContents.Should().NotBeEmpty()
						// don't carry over minimum_master_nodes
						.And.NotContain("discovery.zen.minimum_master_nodes")
						.And.Contain("cluster.initial_master_nodes");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.ClusterName.Should().Be("x");
					s.NodeName.Should().Be("nodex");
					s.InitialMasterNodes.Should().BeEquivalentTo(new List<string> { "nodex" });
				}
			);
		
		[Fact] void InitialMaster7WontOverrideAlreadySetInitialMasterNodes() => 
			DefaultValidModelForTasks(s => s
				.Elasticsearch(es => es.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory))
				.FileSystem(fs =>
				{
					var yamlLocation = Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml");
					fs.AddFile(yamlLocation, new MockFileData(@"cluster.initial_master_nodes: [nodey]"));
					return fs;
				})
			)
			.OnStep(m => m.ConfigurationModel, s =>
			{
				s.ClusterName = "x";
				s.NodeName = "nodex";
				s.InitialMaster.Should().BeFalse();
				s.InitialMaster = true;
			})
			.AssertTask(
				(m, s, fs) => new EditElasticsearchYamlTask(m, s, fs),
				(m, t) =>
				{
					var dir = m.LocationsModel.ConfigDirectory;
					var yaml = Path.Combine(dir, "elasticsearch.yml");
					var yamlContents = t.FileSystem.File.ReadAllText(yaml);
					// validate we are writing the yaml file in 7.0 format
					yamlContents.Should().NotBeEmpty()
						// don't carry over minimum_master_nodes
						.And.NotContain("discovery.zen.minimum_master_nodes")
						.And.Contain("cluster.initial_master_nodes");
					var config = ElasticsearchYamlConfiguration.FromFolder(dir, t.FileSystem);
					var s = config.Settings;
					s.ClusterName.Should().Be("x");
					s.NodeName.Should().Be("nodex");
					s.InitialMasterNodes.Should().BeEquivalentTo(new List<string> { "nodey" });
				}
			);
		

	}
}
