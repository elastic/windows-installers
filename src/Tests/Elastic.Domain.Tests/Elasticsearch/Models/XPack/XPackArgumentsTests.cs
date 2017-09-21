using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.XPack
{
	public class XPackArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void CanPassTrialLicense() => Argument(nameof(XPackModel.XPackLicense), "Trial", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.Contain("x-pack");
		});
		
		[Fact] void CanPassBasicLicense() => Argument(nameof(XPackModel.XPackLicense), "Basic", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.Contain("x-pack");
		});
		
		[Fact] void CanPassEmptyLicense() => Argument(nameof(XPackModel.XPackLicense), "", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(null);
			m.PluginsModel.Plugins.Should().BeEmpty();
		});
		
		[Fact] void BasicWithMultiplePluginsContainingXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Basic))
			.Argument(nameof(PluginsModel.Plugins), "x-pack, analysis-icu")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.Contain("x-pack");
		});
		
		[Fact] void TrialWithMultiplePluginsContainingXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Trial))
			.Argument(nameof(PluginsModel.Plugins), "x-pack, analysis-icu")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.Contain("x-pack");
		});
		
		[Fact] void BasicWithMultiplePluginsNotContainingXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Basic), "")
			.Argument(nameof(PluginsModel.Plugins), "analysis-icu, analysis-phonetic")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(null);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.NotContain("x-pack");
		});
		
		[Fact] void TrialWithMultiplePluginsNotContainingXPack() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Trial), "")
			.Argument(nameof(PluginsModel.Plugins), "analysis-icu, analysis-phonetic")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(null);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(2).And.NotContain("x-pack");
		});
		
		[Fact] void EmptyLicenseWithXpackInPluginsAutoSelectsBasicLicense() => 
			Argument(nameof(XPackModel.XPackLicense), "", nameof(XPackLicenseMode.Basic))
			.Argument(nameof(PluginsModel.Plugins), "x-pack")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.Contain("x-pack");
		});
		
		[Fact] void EmptyPluginsStillInjectsXpackWhenXPackLicenseIsPassed() => 
			Argument(nameof(XPackModel.XPackLicense), "trial" , nameof(XPackLicenseMode.Trial))
			.Argument(nameof(PluginsModel.Plugins), "", "x-pack")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.Plugins.Should().NotBeEmpty().And.HaveCount(1).And.Contain("x-pack");
		});
	}
}
