using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class NoopWixStateProvider : IWixStateProvider
	{
		public SemVersion CurrentVersion => null;
		public SemVersion ExistingVersion => null;
	}
}
