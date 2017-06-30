using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class NoopWixStateProvider : IWixStateProvider
	{
		public SemVersion CurrentVersion => null;
		public bool CurrentlyInstalling => true;
		public SemVersion ExistingVersion => null;
	}
}
