using System.Linq;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.XPack
{
	public class BasicLicenseModelTester : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;
		
		public BasicLicenseModelTester()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.PluginsModel)
				.OnStep(m => m.PluginsModel, m =>
				{
					var xPackPlugin = m.AvailablePlugins.First(p => p.PluginType == PluginType.XPack);
					xPackPlugin.Selected = true;
				})
				.IsValidOnStep(m => m.PluginsModel)
				.ClickNext();
		}

		[Fact] void DefaultLicenseIsBasic() => this._model
			.OnStep(m => m.XPackModel, step => 
			{
				step.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			})
			.CanClickNext();
			
	}
}
