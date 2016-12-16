using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
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
			optsFile.Save();

			var fileContentsAfterSave = fs.File.ReadAllText(_path);
			fileContentsAfterSave.Replace("\r", "").Should().Be(yaml.Replace("\r", ""));

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
