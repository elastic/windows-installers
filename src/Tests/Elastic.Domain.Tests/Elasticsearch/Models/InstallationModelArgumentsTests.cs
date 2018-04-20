using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models
{
	public class InstallationModelArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact]
		void PluginsFromZip() => Argument(nameof(PluginsModel.Plugins), @"C:\ingest-attachment.zip,C:\ingest-geoip.zip", (m, v) =>
		{
			m.PluginsModel.AvailablePlugins.Should().Contain(p => p.Url == "ingest-geoip" && p.Selected);
			m.PluginsModel.AvailablePlugins.Should().Contain(p => p.Url == "ingest-attachment" && p.Selected);
			m.PluginsModel.Plugins.Should().Contain(@"C:\ingest-geoip.zip").And.Contain(@"C:\ingest-attachment.zip");
		});

		[Fact]
		void PluginsFromZipWithFileProtocol() => 
			Argument(nameof(PluginsModel.Plugins), @"file:///C:\ingest-attachment.zip,file:///C:\ingest-geoip.zip", (m, v) =>
			{
				m.PluginsModel.AvailablePlugins.Should().Contain(p => p.Url == "ingest-geoip" && p.Selected);
				m.PluginsModel.AvailablePlugins.Should().Contain(p => p.Url == "ingest-attachment" && p.Selected);
				m.PluginsModel.Plugins.Should().Contain(@"file:///C:\ingest-geoip.zip")
					.And.Contain(@"file:///C:\ingest-attachment.zip");
			});

		[Fact]
		void PluginsFromName() => Argument(nameof(PluginsModel.Plugins), @"ingest-attachment,ingest-geoip", (m, v) =>
		{
			m.PluginsModel.AvailablePlugins.Should().Contain(p => p.Url == "ingest-geoip" && p.Selected);
			m.PluginsModel.AvailablePlugins.Should().Contain(p => p.Url == "ingest-attachment" && p.Selected);
			m.PluginsModel.Plugins.Should().Contain("ingest-geoip").And.Contain("ingest-attachment");
		});
	}
}
