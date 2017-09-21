using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.XPack
{
	public class BasicLicenseArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void CanPassBasicLicense() => Argument(nameof(XPackModel.XPackLicense), "basic", "Basic", (m, v) =>
		{
			m.XPackModel.XPackLicense.Should().Be(XPackLicenseMode.Basic);
			m.XPackModel.IsValid.Should().BeTrue("{0}", m);
		});
	}
}
