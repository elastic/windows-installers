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
	public class EditElasticsearchYamlConfigurationModelTaskTests : InstallationModelTestBase
	{
		[Theory]
		[InlineData("x", "y", false, false, false, false, "xyz", 80, 9300, 3)]
		[InlineData("cluster y", "node z", true, true, true, true, null, null, null, null)]
		[InlineData("unicode: ΣΩ", "123", true, false, true, false, null, 9200, 9300, null)]
		[InlineData("test-cluster", "test-node", true, false, true, false, null, 9200, null, 2)]
		[InlineData("", "", false, true, false, true, "test.com", null, 9300, 5)]
		void CustomConfigValues(string clusterName, string nodeName, bool masterNode, bool dataNode, bool ingestNode, bool lockMemory, string networkHost, int? httpPort, int? transportPort, int? minMasterNodes)
				=> DefaultValidModelForTasks(s => s
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
				s.ClusterName = clusterName;
				s.NodeName = nodeName;
				s.MasterNode = masterNode;
				s.DataNode = dataNode;
				s.IngestNode = ingestNode;
				s.LockMemory = lockMemory;
				s.NetworkHost = networkHost;
				s.HttpPort = httpPort;
				s.TransportPort = transportPort;
				s.UnicastNodes = new ReactiveUI.ReactiveList<string>
				{
					"localhost", "192.2.3.1:9301"
				};
				if (minMasterNodes.HasValue)
					s.MinimumMasterNodes = minMasterNodes.Value;
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
					s.ClusterName.Should().Be(clusterName);
					s.NodeName.Should().Be(nodeName);
					s.MasterNode.Should().Be(masterNode);
					s.IngestNode.Should().Be(ingestNode);
					s.DataNode.Should().Be(dataNode);
					s.MemoryLock.Should().Be(lockMemory);
					s.NetworkHost.Should().Be(networkHost);
					s.HttpPort.Should().Be(httpPort);
					if (httpPort.HasValue)
						s.HttpPortString.Should().Be(httpPort.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
					else
						s.HttpPortString.Should().BeNull();
					s.TransportTcpPort.Should().Be(transportPort);
					if (transportPort.HasValue)
						s.TransportTcpPortString.Should().Be(transportPort.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
					else
						s.TransportTcpPortString.Should().BeNull();
					s.UnicastHosts.Should().BeEquivalentTo(new List<string>
					{
						"localhost", "192.2.3.1:9301"
					});
					if (minMasterNodes.HasValue)
						s.MinimumMasterNodes.Should().Be(minMasterNodes.Value);
					else
						s.MinimumMasterNodes.Should().Be(0); //ConfigurationModel.MinimumMasterNodes is of type int
				}
			);

	}
}
