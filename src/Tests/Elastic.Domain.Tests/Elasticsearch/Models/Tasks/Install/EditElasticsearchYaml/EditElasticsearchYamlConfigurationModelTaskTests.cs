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
