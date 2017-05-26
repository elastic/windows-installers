using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class IdentifiersArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void NodeNameEmpty() => Argument(nameof(ConfigurationModel.NodeName), "", ConfigurationModel.DefaultNodeName, (m, v) =>
		{
			//nodename being a SetPropertyActionArgumentAttribute can not be unset
			m.ConfigurationModel.NodeName.Should().NotBeEmpty();
			m.ConfigurationModel.IsValid.Should().BeTrue();
		});

		[Fact] void NodeName() => Argument(nameof(ConfigurationModel.NodeName), "my-node", (m, v) =>
		{
			m.ConfigurationModel.NodeName.Should().Be(v);
			m.ConfigurationModel.IsValid.Should().BeTrue();
		});

		[Fact] void ClusterNameEmpty() => Argument(nameof(ConfigurationModel.ClusterName), "", (m, v) =>
		{
			m.ConfigurationModel.ClusterName.Should().BeEmpty();
			m.ConfigurationModel.IsValid.Should().BeFalse();
		});

		[Fact] void ClusterName() => Argument(nameof(ConfigurationModel.ClusterName), "my-node", (m, v) =>
		{
			m.ConfigurationModel.ClusterName.Should().Be(v);
			m.ConfigurationModel.IsValid.Should().BeTrue();
		});

	}
}
