using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.XPack
{
	public class XPackLicenseArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void CanPassTrialLicense() => Argument(nameof(XPackModel.XPackLicense), "Trial", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.XPackEnabled.Should().BeTrue();
		});
		
		[Fact] void CanPassBasicLicense() => Argument(nameof(XPackModel.XPackLicense), "Basic", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.XPackEnabled.Should().BeTrue();
		});
		
		[Fact] void CanPassEmptyLicense() => Argument(nameof(XPackModel.XPackLicense), "", "Basic", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackModel.DefaultXPackLicenseMode);
			m.PluginsModel.Plugins.Should().BeEmpty();
		});
		
		[Fact] void BasicWithMultiplePluginsContainingXPackIgnoresXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Basic))
			.Argument(nameof(PluginsModel.Plugins), "x-pack, analysis-icu", "analysis-icu")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.Contain("analysis-icu");
		});
		
		[Fact] void TrialWithMultiplePluginsContainingXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Trial))
			.Argument(nameof(PluginsModel.Plugins), "x-pack, analysis-icu", "analysis-icu")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.NotContain("x-pack");
		});
		
		[Fact] void BasicWithMultiplePluginsNotContainingXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Basic), "Basic")
			.Argument(nameof(PluginsModel.Plugins), "analysis-icu, analysis-phonetic")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.NotContain("x-pack");
		});
		
		[Fact] void TrialWithMultiplePluginsNotContainingXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Trial), "Trial")
			.Argument(nameof(PluginsModel.Plugins), "analysis-icu, analysis-phonetic")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.NotContain("x-pack");
		});
		
		[Fact] void EmptyLicenseWithXpackInPluginsAutoSelectsBasicLicense() => 
			Argument(nameof(XPackModel.XPackLicense), "", nameof(XPackLicenseMode.Basic))
			.Argument(nameof(PluginsModel.Plugins), "x-pack", "")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.Plugins.Should().BeEmpty();
			m.PluginsModel.XPackEnabled.Should().BeTrue();
		});
		
		[Fact] void EmptyLicenseWithXpackInPluginsAutoSelectsBasicLicenseReverse() => 
			Argument(nameof(PluginsModel.Plugins), "x-pack", "")
			.Argument(nameof(XPackModel.XPackLicense), "", nameof(XPackLicenseMode.Basic))
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.Plugins.Should().BeEmpty();
			m.PluginsModel.XPackEnabled.Should().BeTrue();
		});
		
		[Fact] void WhenPassingNoPluginsXPackIsNoLongerAutomaticallySelected() => 
			Argument(nameof(XPackModel.XPackLicense), "trial" , nameof(XPackLicenseMode.Trial))
			.Argument(nameof(PluginsModel.Plugins), "", "")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.Plugins.Should().BeEmpty();
			m.PluginsModel.XPackEnabled.Should().BeTrue();
		});
	}
}
