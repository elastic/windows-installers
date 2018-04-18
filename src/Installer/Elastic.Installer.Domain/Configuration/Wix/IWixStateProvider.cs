using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public interface IWixStateProvider
	{
		SemVersion PreviousVersion { get; }
		SemVersion InstallerVersion { get; }
	}
}
