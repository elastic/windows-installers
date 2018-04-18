using Elastic.Installer.Domain.Configuration.Wix;
using Semver;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockWixStateProvider : IWixStateProvider
	{
		public MockWixStateProvider()
		{
			this.InstallerVersion = "5.0.0-alpha5";
		}

		public SemVersion PreviousVersion { get; set; }

		public SemVersion InstallerVersion { get; set; }
	}
}
