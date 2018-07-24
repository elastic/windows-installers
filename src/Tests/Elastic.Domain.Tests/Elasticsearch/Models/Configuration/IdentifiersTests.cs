using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class IdentifiersTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public IdentifiersTests()
		{
			this._model = DefaultValidModel()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.ConfigurationModel);
		}

		[Fact] void ClusterNameNotEmpty() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.ClusterName = null; })
			.CanClickNext(false)
			.OnStep(m => m.ConfigurationModel, step => { step.ClusterName = ""; })
			.CanClickNext(false);

		[Fact] void NodeNameNotEmpty() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.NodeName = null; })
			.CanClickNext(false)
			.OnStep(m => m.ConfigurationModel, step => { step.NodeName = ""; })
			.CanClickNext(false);

		private readonly string _clusterName = "my-configured-cluster with spaces for some reason";
		private readonly string _nodeName = "弹性搜索 will no longer support this in 5.x but since its valid yaml lets pretend this is a valid node name";
		[Fact] void SeedsFromElasticsearchYaml() => WithExistingElasticsearchYaml($@"cluster.name: {_clusterName}
node.name: {_nodeName}
"
				)
				.OnStep(m => m.ConfigurationModel, step =>
				{
					step.ClusterName.Should().Be(_clusterName);
					step.NodeName.Should().Be(_nodeName);
				});

			
	}
}
