using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public interface IWixStateProvider
	{
		bool CurrentlyInstalling { get; }
		SemVersion ExistingVersion { get; }
		SemVersion CurrentVersion { get; }
	}
}
