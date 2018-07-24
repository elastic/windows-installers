using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install
{
	public class InstallPluginTaskTests : InstallationModelTestBase
	{
		[Fact] void InstallByDefault()
		{
			var model = DefaultValidModelForTasks();
			model.AssertTask(
				(m, s, fs) => new InstallPluginsTask(m, s, fs),
				(m, t) =>
				{
					t.PluginState.InstalledAfter.Should().BeEmpty();
				}
			);
		}
		[Fact] void PreviouslyInstalledPluginSelectionCarriesOver()
		{
			var model = DefaultValidModelForTasks(s=>s
				.Wix("5.1.0", "5.0.0")
				.PreviouslyInstalledPlugins("ingest-geoip", "analysis-kuromoji")
			);
			model.AssertTask(
				(m, s, fs) => new InstallPluginsTask(m, s, fs),
				(m, t) =>
				{
					t.PluginState.InstalledAfter.Should()
						.NotBeEmpty()
						.And.HaveCount(2)
						.And.Contain("analysis-kuromoji")
						.And.Contain("ingest-geoip");
				}
			);
		}
		[Fact] void PreviouslyInstalledPluginSelectionCarriesOverEvenWhenEmpty()
		{
			var model = DefaultValidModelForTasks(s=>s
				.Wix("5.1.0", "5.0.0")
				.PreviouslyInstalledPlugins()
			);
			model.AssertTask(
				(m, s, fs) => new InstallPluginsTask(m, s, fs),
				(m, t) =>
				{
					t.PluginState.InstalledAfter.Should().BeEmpty();
				}
			);
		}
	}
}
