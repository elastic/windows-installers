using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class NetworkArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void HttpPort() => Argument(nameof(ConfigurationModel.HttpPort), 80, (m, v) =>
		{
			m.ConfigurationModel.HttpPort.Should().Be(v);
		});

		[Fact] void HttpPortEmpty() => Argument(nameof(ConfigurationModel.HttpPort), "", (m, v) =>
		{
			m.ConfigurationModel.HttpPort.Should().NotHaveValue();
		});

		[Fact] void HttpPortExceeded() => Argument(nameof(ConfigurationModel.HttpPort), int.MaxValue, (m, v) =>
		{
			m.ConfigurationModel.HttpPort.Should().Be(v);
			m.ConfigurationModel.IsValid.Should().BeFalse();
		});

		[Fact] void TransportPort() => Argument(nameof(ConfigurationModel.TransportPort), 9090, (m, v) =>
		{
			m.ConfigurationModel.TransportPort.Should().Be(v);
		});
		[Fact] void TransportPortSameAsHttp() => Argument(nameof(ConfigurationModel.TransportPort), 9200, (m, v) =>
		{
			m.ConfigurationModel.TransportPort.Should().Be(v);
		});

		[Fact] void TransportPortEmpty() => Argument(nameof(ConfigurationModel.TransportPort), "", (m, v) =>
		{
			m.ConfigurationModel.TransportPort.Should().NotHaveValue();
		});

		[Fact] void NetworkHostEmpty() => Argument(nameof(ConfigurationModel.NetworkHost), "", (m, v) =>
		{
			m.ConfigurationModel.NetworkHost.Should().BeEmpty();
		});

		[Fact] void NetworkHost() => Argument(nameof(ConfigurationModel.NetworkHost), "localhost" , (m, v) =>
		{
			m.ConfigurationModel.NetworkHost.Should().Be(v);
		});

	}
}
