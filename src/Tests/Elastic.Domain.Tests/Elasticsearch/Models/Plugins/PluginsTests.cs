using System.Linq;
using Elastic.Installer.Domain.Model.Base.Plugins;
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

		[Fact] void SelectingXPackIsPropagated() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.XPackEnabled.Should().BeTrue();

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

		[Theory]
		[InlineData(79)]
		[InlineData(65536)]
		void HttpProxyHostInvalidOutsideOfRange(int v) => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpProxyHost = "localhost";
				step.HttpProxyPort = v;
			})
			.IsInvalidOnStep(m => m.PluginsModel, errors => errors
				.ShouldHaveErrors(string.Format(PluginsModelValidator.InvalidHttpProxyPort, PluginsModel.HttpPortMinimum, PluginsModel.PortMaximum))
			)
			.CanClickNext(false);

		[Fact] void HttpProxyHostRequiredWhenPortSpecified() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpProxyPort = 80;
			})
			.IsInvalidOnStep(m => m.PluginsModel, errors => errors
				.ShouldHaveErrors(PluginsModelValidator.HttpProxyHostRequiredWhenPortSpecified)
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

		[Fact]
		void HttpsProxyHostRequiredWhenPortSpecified() => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpsProxyPort = 443;
			})
			.IsInvalidOnStep(m => m.PluginsModel, errors => errors
				.ShouldHaveErrors(PluginsModelValidator.HttpsProxyHostRequiredWhenPortSpecified)
			)
			.CanClickNext(false);


		[Theory]
		[InlineData(442)]
		[InlineData(65536)]
		void HttpsProxyHostInvalidOutsideOfRange(int v) => this._model
			.OnStep(m => m.PluginsModel, step =>
			{
				step.HttpsProxyHost = "localhost";
				step.HttpsProxyPort = v;
			})
			.IsInvalidOnStep(m => m.PluginsModel, errors => errors
				.ShouldHaveErrors(string.Format(PluginsModelValidator.InvalidHttpsProxyPort, PluginsModel.HttpsPortMinimum, PluginsModel.PortMaximum))
			)
			.CanClickNext(false);
	}
}
