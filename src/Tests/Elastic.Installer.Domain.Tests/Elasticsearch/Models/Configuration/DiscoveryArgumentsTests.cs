using Elastic.Installer.Domain.Elasticsearch.Model.Config;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class DiscoveryArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void UnicastNodesEmpty() => Argument(nameof(ConfigurationModel.UnicastNodes), "", (m, v) =>
		{
			m.ConfigurationModel.UnicastNodes.Should().BeEmpty();
		});

		[Fact] void UnicastNodesSingle() => Argument(nameof(ConfigurationModel.UnicastNodes), "my-domain:9200", (m, v) =>
		{
			m.ConfigurationModel.UnicastNodes.Should().NotBeEmpty().And.HaveCount(1).And.Contain(v);
		});

		[Fact] void UnicastNodesMultipe() => Argument(
			nameof(ConfigurationModel.UnicastNodes),
			"my-domain:9200,,,  192.3.3.1:8080",
			"my-domain:9200, 192.3.3.1:8080",
			(m, v) =>
		{
			m.ConfigurationModel.UnicastNodes.Should().NotBeEmpty().And.HaveCount(2).And.Contain("my-domain:9200");
		});

		[Fact] void MinimumMasterNodes() => Argument(nameof(ConfigurationModel.MinimumMasterNodes), 2, (m, v) =>
		{
			m.ConfigurationModel.MinimumMasterNodes.Should().Be(2);
			m.ConfigurationModel.IsValid.Should().BeTrue();
		});

		[Fact] void MinimumMasterNodesNegative() => Argument(nameof(ConfigurationModel.MinimumMasterNodes), -1, (m, v) =>
		{
			m.ConfigurationModel.MinimumMasterNodes.Should().Be(-1);
			m.ConfigurationModel.IsValid.Should().BeFalse();
		});

	}
}
