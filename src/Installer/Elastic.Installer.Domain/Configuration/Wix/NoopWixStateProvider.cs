using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class NoopWixStateProvider : IWixStateProvider
	{
		public SemVersion ExistingVersion { get; set; }

		public SemVersion CurrentVersion { get; set; }
	}
}