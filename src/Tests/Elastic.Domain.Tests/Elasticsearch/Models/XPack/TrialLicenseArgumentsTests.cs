using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.XPack
{
	public class TrialLicenseArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void CanPassTrialLicense() => Argument(nameof(XPackModel.XPackLicense), "trIal", "Trial", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.PluginsModel.XPackEnabled.Should().BeTrue();
			m.XPackModel.IsValid.Should().BeFalse("{0}", m); //needs passwords by default
		});
		
		[Fact] void SignalUsersWillBeGeneratedLater() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Trial))
			.Argument(nameof(XPackModel.SkipSettingPasswords), "1", "true")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.XPackModel.SkipSettingPasswords.Should().BeTrue();
			m.XPackModel.IsValid.Should().BeTrue("{0}", m);
		});
		
		[Fact] void InstallTrialButWithoutSecurity() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Trial))
			.Argument(nameof(XPackModel.XPackSecurityEnabled), "0", "false")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.XPackModel.XPackSecurityEnabled.Should().BeFalse();
			m.XPackModel.IsValid.Should().BeTrue("{0}", m);
		});
		
		[Fact] void CanPassSystemUserPasswords() => 
			Argument(nameof(XPackModel.XPackLicense), nameof(XPackLicenseMode.Trial))
			.Argument(nameof(XPackModel.ElasticUserPassword), "es-pass")
			.Argument(nameof(XPackModel.KibanaUserPassword), "ki-pass")
			.Argument(nameof(XPackModel.LogstashSystemUserPassword), "ls-pass")
			.Assert(m =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Trial);
			m.XPackModel.IsValid.Should().BeTrue("{0}", m);
			m.XPackModel.XPackSecurityEnabled.Should().BeTrue();
			m.XPackModel.SkipSettingPasswords.Should().BeFalse();
			m.XPackModel.ElasticUserPassword.Should().Be("es-pass");
			m.XPackModel.KibanaUserPassword.Should().Be("ki-pass");
			m.XPackModel.LogstashSystemUserPassword.Should().Be("ls-pass");
		});
	}
}
