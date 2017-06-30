using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Plugins
{
	public class PluginsTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public PluginsTests()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.PluginsModel);
		}

		[Fact] void XPackNotSelectedByDefault() => this._model
			.OnStep(m => m.PluginsModel, step => 
			{
				step.AvailablePlugins.Should().NotContain(a => a.Url == "x-pack" && a.Selected);
			})
			.CanClickNext();


		[Fact] void IngestPluginsNotSelectedByDefault() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.AvailablePlugins.Should().NotContain(a => a.Url == "ingest-attachment" && a.Selected);
				step.AvailablePlugins.Should().NotContain(a => a.Url == "ingest-geoip" && a.Selected);
			})
			.CanClickNext();
	}
}
