using System.Linq;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Plugins
{
	public class PluginsArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void PluginsReset() => Argument(nameof(PluginsModel.Plugins), "", (m, v) =>
		{
			m.PluginsModel.Plugins.Should().BeEmpty();
			m.PluginsModel.AvailablePlugins.Count(p => p.Selected).Should().Be(0);
		});

		[Fact] void PluginsSingleValue() => Argument(nameof(PluginsModel.Plugins), "analysis-icu", (m, v) =>
		{
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.Contain("analysis-icu");
			m.PluginsModel.AvailablePlugins.Count(p => p.Selected).Should().Be(1);
		});

		[Fact] void PluginsMultipleValues() => Argument(nameof(PluginsModel.Plugins), "analysis-icu, analysis-phonetic", (m, v) =>
		{
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.Contain("analysis-phonetic");
			m.PluginsModel.AvailablePlugins.Count(p => p.Selected).Should().Be(2);
		});

		[Fact] void PluginsMultipleValuesIgnoresUnknown() => Argument(
			nameof(PluginsModel.Plugins), 
			"analysis-icu, analysis-phonetic, badplugin", 
			"analysis-icu, analysis-phonetic", 
			(m, v) =>
		{
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.Contain("analysis-phonetic");
			m.PluginsModel.AvailablePlugins.Count(p => p.Selected).Should().Be(2);
		});
		
		[Fact] void ProvidingXPackInTheListOfPluginsIsIgnored() => Argument(nameof(PluginsModel.Plugins), "x-pack", "", (m, v) =>
		{
			m.PluginsModel.Plugins.Should().BeEmpty();
			m.XPackModel.IsRelevant.Should().BeTrue();
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
		});

		[Fact] void PluginsHttpProxyHost() => Argument(
			nameof(PluginsModel.HttpProxyHost),
			"localhost",
			(m, v) =>
			{
				m.PluginsModel.HttpProxyHost.Should().NotBeNullOrEmpty().And.Be("localhost");
			});

		[Fact] void PluginsHttpProxyPort() => Argument(
			nameof(PluginsModel.HttpProxyPort),
			"8888",
			(m, v) =>
			{
				m.PluginsModel.HttpProxyPort.Should().NotBeNull().And.Be(8888);
			});

		[Fact] void PluginsHttpsProxyHost() => Argument(
			nameof(PluginsModel.HttpsProxyHost),
			"localhost",
			(m, v) =>
			{
				m.PluginsModel.HttpsProxyHost.Should().NotBeNullOrEmpty().And.Be("localhost");
			});

		[Fact] void PluginsHttpsProxyPort() => Argument(
			nameof(PluginsModel.HttpsProxyPort),
			"8888",
			(m, v) =>
			{
				m.PluginsModel.HttpsProxyPort.Should().NotBeNull().And.Be(8888);
			});

	}
}
