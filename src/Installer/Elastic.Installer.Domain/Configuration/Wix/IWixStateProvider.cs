using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public interface IWixStateProvider
	{
		SemVersion ExistingVersion { get; }
		SemVersion CurrentVersion { get; }
	}
}
