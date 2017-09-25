using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
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

		[Fact] void ValidHttpProxyHost() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpProxyHost = "localhost";
			})
			.CanClickNext();

		[Fact] void ValidHttpsProxyHost() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpsProxyHost = "localhost";
			})
			.CanClickNext();

		[Fact] void InvalidHttpProxyHost() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpProxyHost = "@";
			})
			.IsInvalidOnStep(m => m.PluginsModel, errors => errors
				.ShouldHaveErrors(PluginsModelValidator.InvalidHttpProxyHost)
			)
			.CanClickNext(false);

		[Fact] void InvalidHttpsProxyHost() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpsProxyHost = "@";
			})
			.IsInvalidOnStep(m => m.PluginsModel, errors => errors
				.ShouldHaveErrors(PluginsModelValidator.InvalidHttpsProxyHost)
			)
			.CanClickNext(false);
	}
}
