using Elastic.Installer.Domain.Configuration.Wix;
using Semver;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockWixStateProvider : IWixStateProvider
	{
		public MockWixStateProvider()
		{
			this.CurrentVersion = TestSetupStateProvider.DefaultTestVersion;
		}

		public SemVersion UpgradeFromVersion { get; set; }

		public SemVersion CurrentVersion { get; set; }
	}
}
