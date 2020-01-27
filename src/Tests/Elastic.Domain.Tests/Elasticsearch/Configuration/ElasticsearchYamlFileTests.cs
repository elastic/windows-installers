using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration
{
	public class ElasticsearchYamlFileTests
	{
		private string _path = "C:\\Java\\elasticsearch.yaml";

		[Fact] void KnownSettingsAreReadCorrectly()
		{
			var nodeName = "DESKTOP-O4M5B2Q";
			var clusterName = "my-cluster";
			var networkHost = "127.0.0.1";
			var folder = @"C:\ProgramData\Elastic\Elasticsearch\";
			var yaml = $@"bootstrap.memory_lock: true
cluster.name: {clusterName}
network.host: {networkHost}
node.data: true
node.ingest: true
node.master: true
node.max_local_storage_nodes: 1
node.name: {nodeName}
path.data: {folder}data
path.logs: {folder}logs
xpack.license.self_generated.type: trial
xpack.security.enabled: false
xpack.random_setting: something
";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.NodeName.Should().Be(nodeName);
			settings.ClusterName.Should().Be(clusterName);
			settings.NetworkHost.Should().Be(networkHost);
			settings.DataNode.Should().BeTrue();
			settings.IngestNode.Should().BeTrue();
			settings.MemoryLock.Should().BeTrue();
			settings.DataPath.Should().Be(folder + "data");
			settings.LogsPath.Should().Be(folder + "logs");
			settings.MaxLocalStorageNodes.Should().Be(1);
			settings.XPackSecurityEnabled.Should().BeFalse();
			settings.XPackLicenseSelfGeneratedType.Should().Be(nameof(XPackLicenseMode.Trial).ToLowerInvariant());
			settings.Keys.Where(k => k.StartsWith("xpack")).Should().HaveCount(1);
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(_path);
			fileContentsAfterSave.Replace("\r", "").Should().Be(yaml.Replace("\r", ""));

		}
		
		[Fact] void UnicastHostsAreStillRead()
		{
			var yaml = $@"discovery.zen.ping.unicast.hosts: [host1, host2]";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.UnicastHosts.Should().NotBeEmpty().And.HaveCount(2);
			optsFile.Save();
		}
		
		[Fact] void SeedHostsAreRead()
		{
			var yaml = $@"discovery.seed_hosts: [host1, host2]";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.SeedHosts.Should().NotBeEmpty().And.HaveCount(2);
			optsFile.Save();
		}

		[Fact] void SeedHostsAreReadFromStringSingle()
		{
			var yaml = $@"discovery.seed_hosts: host1";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.SeedHosts.Should().NotBeEmpty().And.HaveCount(1);
			optsFile.Save();
		}

		[Fact] void SeedHostsAreReadFromStringCommaSeparated()
		{
			var yaml = $@"discovery.seed_hosts: host1, host2";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.SeedHosts.Should().NotBeEmpty().And.HaveCount(2);
			optsFile.Save();
		}

		[Fact] void InitialMasterNodeAreRead()
		{
			var yaml = $@"InitialMasterNode: [host1, host2]";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.InitialMasterNodes.Should().NotBeEmpty().And.HaveCount(2);
			optsFile.Save();
		}

		[Fact] void InitialMasterNodesAreReadFromStringSingle()
		{
			var yaml = $@"cluster.initial_master_nodes: host1";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.InitialMasterNodes.Should().NotBeEmpty().And.HaveCount(1);
			optsFile.Save();
		}

		[Fact] void InitialMasterNodesAreReadFromStringCommaSeparated()
		{
			var yaml = $@"cluster.initial_master_nodes: host1, host2";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.InitialMasterNodes.Should().NotBeEmpty().And.HaveCount(2);
			optsFile.Save();
		}

		[Fact] void UnknownSettingsAreNotLost()
		{
			var nodeName = "DESKTOP-O4M5B2Q";
			var clusterName = "my-cluster";
			var yaml = $@"cluster.name: {clusterName}
node.name: {nodeName}
some.plugin.setting: true
";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			var settings = optsFile.Settings;
			settings.NodeName.Should().Be(nodeName);
			settings.ClusterName.Should().Be(clusterName);
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(_path);
			fileContentsAfterSave.Replace("\r", "").Should().Be(yaml.Replace("\r", ""));

		}

		[Fact] void UnderstandsObjectNotation()
		{
			var clusterName = "my-cluster";
			var yaml = $@"cluster:
    name: {clusterName}
    blocks:
        read_only: true
";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			optsFile.Settings.ClusterName.Should().Be(clusterName);
			optsFile.Settings.ClusterName = "x";
			optsFile.Settings["cluster.blocks.read_only"] = false;
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(_path);
			var updatedYaml = $@"cluster.name: x
cluster.blocks.read_only: false
";
			fileContentsAfterSave.Replace("\r", "").Should().Be(updatedYaml.Replace("\r", ""));
		}

		[Fact] void ReflectsChanges()
		{
			var clusterName = "my-cluster";
			var yaml = $@"cluster.name: {clusterName}
some.plugin.setting: true
";
			var fs = FakeElasticsearchYaml(yaml);
			var optsFile = new ElasticsearchYamlConfiguration(_path, fs);
			optsFile.Settings.ClusterName = "x";
			optsFile.Settings["some.plugin.setting"] = false;
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(_path);
			var updatedYaml = $@"cluster.name: x
some.plugin.setting: false
";
			fileContentsAfterSave.Replace("\r", "").Should().Be(updatedYaml.Replace("\r", ""));
		}
		private MockFileSystem FakeElasticsearchYaml(string yaml)
		{
			var fs = new MockFileSystem(new Dictionary<string, MockFileData>
			{
				{_path, new MockFileData(yaml)}
			});
			return fs;
		}
	}
}
