using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class NetworkTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public NetworkTests()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.ConfigurationModel);
		}

		[Fact]
		void NetworkSectionIsCompletelyOptional() => this._model
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.NetworkHost.Should().BeNull();
				step.HttpPort.Should().Be(9200);
				step.TransportPort.Should().Be(9300);
				step.NetworkHost = "localhost";
				step.HttpPort = null;
				step.TransportPort = null;
			})
			.IsValidOnStep(m => m.ConfigurationModel)
			.ClickRefresh()
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.NetworkHost.Should().BeNull();
				step.HttpPort.Should().Be(9200);
				step.TransportPort.Should().Be(9300);
			})
			.CanClickNext();

		[Fact] void CanNotSetHttpPortBeyondPortRange() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.HttpPort = int.MaxValue; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				string.Format(ConfigurationModelValidator.PortMaximum, ConfigurationModel.PortMaximum)
			));

		[Fact] void CanNotSetHttpPortToTransportPort() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.HttpPort = step.TransportPort; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				string.Format(ConfigurationModelValidator.EqualPorts, this._model.InstallationModel.ConfigurationModel.HttpPort)
			));

		[Fact] void CanNotSetTransportPortToHttpPort() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.TransportPort = step.HttpPort; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				string.Format(ConfigurationModelValidator.EqualPorts, this._model.InstallationModel.ConfigurationModel.TransportPort)
			));

		[Fact] void CanNotSetTransportPortBeyondPortRange() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.TransportPort = int.MaxValue; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				string.Format(ConfigurationModelValidator.PortMaximum, ConfigurationModel.PortMaximum)
			));

		[Fact] void CanNotSetTransportPortBelowPortRange() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.TransportPort = ConfigurationModel.TransportPortMinimum - 1; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				string.Format(ConfigurationModelValidator.TransportPortMinimum, ConfigurationModel.TransportPortMinimum)
			));

		[Fact] void CanNotSetHttpPortBelowPortRange() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.HttpPort = ConfigurationModel.HttpPortMinimum - 1; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				string.Format(ConfigurationModelValidator.HttpPortMinimum, ConfigurationModel.HttpPortMinimum)
			));

		[Fact] void SeedsFromElasticsearchYaml() => WithExistingElasticsearchYaml($@"network.host: __local__
http.port: 9201
transport.tcp.port: 9301
"
				)
				.OnStep(m => m.ConfigurationModel, step =>
				{
					step.NetworkHost.Should().Be("__local__");
					step.HttpPort.Should().Be(9201);
					step.TransportPort.Should().Be(9301);
				});

		/// <summary>
		/// Port ranges not supported in UI but will get written back to elasticsearch.yml
		/// </summary>
		[Fact] void PortRangeInExistingElasticsearchYamlIgnored() => WithExistingElasticsearchYaml($@"network.host: __local__
http.port: 9201-9300
transport.tcp.port: 9301-9400
"
				)
				.OnStep(m => m.ConfigurationModel, step =>
				{
					step.NetworkHost.Should().Be("__local__");
					step.HttpPort.Should().Be(9200);
					step.TransportPort.Should().Be(9300);
				});

	}
}
