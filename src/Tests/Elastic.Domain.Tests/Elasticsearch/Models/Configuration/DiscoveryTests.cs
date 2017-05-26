using System;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class DiscoveryTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public DiscoveryTests()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.ConfigurationModel);
		}

		[Fact] void CanNotSelectNegativeMinimumMasterNodes() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.MinimumMasterNodes = -1; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				ConfigurationModelValidator.NegativeMinimumMasterNodes
			))
			.CanClickNext(false);

		[Fact] void WhenNoUnicastAreSelectedCanNotClickRemove() => this._model
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.UnicastNodes.IsEmpty.Should().BeTrue();
				step.RemoveUnicastNode.CanExecute(null).Should().BeFalse();
			});

		[Fact] void WhenNoUnicastAreSelectedCanClickAdd() => this._model
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.UnicastNodes.IsEmpty.Should().BeTrue();
				step.AddUnicastNode.CanExecute(null).Should().BeTrue();
			});

		[Fact] void CanAddValidNode() => this._model
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.UnicastNodes.IsEmpty.Should().BeTrue();
				var node = "192.168.2.2:9201";
				step.AddUnicastNodeUITask = () => Task.FromResult(node);
				step.AddUnicastNode.Execute(null);
				step.UnicastNodes.IsEmpty.Should().BeFalse();
				step.UnicastNodes.Count.Should().Be(1);
				step.UnicastNodes[0].Should().Be(node);
			});

		[Fact] void CanAddCommaSeparatedValidNodesSkippingEmptyValuesTrimingTheRest() => this._model
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.UnicastNodes.IsEmpty.Should().BeTrue();
				var node = "192.168.2.2:9201    ,192.168.2.3:9201,,192.168.2.4:9201";
				step.AddUnicastNodeUITask = () => Task.FromResult(node);
				step.AddUnicastNode.Execute(null);
				step.UnicastNodes.IsEmpty.Should().BeFalse();
				step.UnicastNodes.Count.Should().Be(3);
				var nodes = node.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var n in nodes)
					step.UnicastNodes.Should().Contain(n.Trim());
			});

		[Fact] void CanRemoveSelectedUnicastNode() => this._model
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.UnicastNodes.IsEmpty.Should().BeTrue();
				var node = "192.168.2.2:9201";
				step.AddUnicastNodeUITask = () => Task.FromResult(node);
				step.AddUnicastNode.Execute(null);
				step.UnicastNodes.IsEmpty.Should().BeFalse();
				step.SelectedUnicastNode = node;
				step.RemoveUnicastNode.Execute(null);
				step.UnicastNodes.IsEmpty.Should().BeTrue();
			});

		private string _unicastHosts = @"[""127.0.0.1"", ""[::1]"", ""192.168.1.2:9301""]";
		[Fact] void SeedsFromElasticsearchYaml() => WithExistingElasticsearchYaml($@"discovery.zen.ping.unicast.hosts: {_unicastHosts}
discovery.zen.minimum_master_nodes: 20
"
				)
				.OnStep(m => m.ConfigurationModel, step =>
				{
					step.UnicastNodes.Should().NotBeEmpty().And.HaveCount(3).And.BeEquivalentTo("127.0.0.1", "[::1]", "192.168.1.2:9301");
					step.MinimumMasterNodes.Should().Be(20);
				});

	}
}
