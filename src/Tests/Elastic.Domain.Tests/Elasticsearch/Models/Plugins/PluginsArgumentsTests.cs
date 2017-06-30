using System.Linq;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
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
		
		[Fact] void PluginsResetsPreviousSelection() => Argument(
			s=>s.PreviouslyInstalledPlugins("x-pack"),
			nameof(PluginsModel.Plugins), "", (m, v) =>
		{
			m.PluginsModel.Plugins.Should().BeEmpty();
			m.PluginsModel.AvailablePlugins.Count(p => p.Selected).Should().Be(0);
		});
		[Fact] void PluginsResetsPreviousSelectionWhenAlreadyInstalled() => Argument(
			setup: s => s.PreviouslyInstalledPlugins("x-pack").Wix(alreadyInstalled:true),
			key: nameof(PluginsModel.Plugins), 
			value: "", 
			assert: (m, v) =>
		{
			m.PluginsModel.Plugins.Should().BeEmpty();
			m.PluginsModel.AvailablePlugins.Count(p => p.Selected).Should().Be(0);
		});
		
		[Fact] void PluginsDefaultLeavesPreviousSelectionWhenAlreadyInstalled() => Argument(
			setup: s => s.PreviouslyInstalledPlugins("x-pack").Wix(alreadyInstalled: true),
			key: nameof(PluginsModel.Plugins), 
			value: PluginsModel.UnchangedMoniker, 
			msiParam: "x-pack",
			assert: (m, v) =>
		{
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.Contain("x-pack");
			m.PluginsModel.AvailablePlugins.Count(p => p.Selected).Should().Be(1);
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

	}
}
