using System.Linq;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class RolesArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void DataNodeFalse() => Argument(nameof(ConfigurationModel.DataNode), false, (m, v) =>
		{
			m.ConfigurationModel.DataNode.Should().BeFalse();
		});

		[Fact] void MasterNodeFalse() => Argument(nameof(ConfigurationModel.MasterNode), false, (m, v) =>
		{
			m.ConfigurationModel.MasterNode.Should().BeFalse();
		});
		[Fact] void IngestNodeFalse() => Argument(nameof(ConfigurationModel.IngestNode), false, (m, v) =>
		{
			m.ConfigurationModel.IngestNode.Should().BeFalse();
			m.PluginsModel.Plugins.Should().HaveCount(1).And.Contain("x-pack");
		});

		[Fact] void IngestNodeTrue() => Argument(nameof(ConfigurationModel.IngestNode), true, (m, v) =>
		{
			m.ConfigurationModel.IngestNode.Should().BeTrue();
			m.PluginsModel.Plugins.Count().Should().BeGreaterThan(1);
		});

	}
}
