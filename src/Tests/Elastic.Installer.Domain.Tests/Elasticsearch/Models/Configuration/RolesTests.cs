using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class RolesTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public RolesTests()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.ConfigurationModel);
		}

		[Fact] void AllRolesEnabledByDefault() => this._model
			.OnStep(m => m.ConfigurationModel, step => 
			{
				step.DataNode.Should().BeTrue();
				step.IngestNode.Should().BeTrue();
				step.MasterNode.Should().BeTrue();
			})
			.CanClickNext();

		[Fact] void SeedsFromElasticsearchYaml() => WithExistingElasticsearchYaml($@"node.data: false
node.master: false
node.ingest: false
"
				)
				.OnStep(m => m.ConfigurationModel, step =>
				{
					step.DataNode.Should().BeFalse();
					step.IngestNode.Should().BeFalse();
					step.MasterNode.Should().BeFalse();
				});
			
	}
}
