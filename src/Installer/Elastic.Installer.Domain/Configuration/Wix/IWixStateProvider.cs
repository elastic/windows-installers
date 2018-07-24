using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public interface IWixStateProvider
	{
		/// <summary> The version we are upgrading from </summary>
		SemVersion UpgradeFromVersion { get; }
		
		/// <summary> The version that is currently being installed </summary>
		SemVersion CurrentVersion { get; }
		
		/// <summary> Indicates the model was instantatiated when the installation is in progresss</summary>
		bool InstallationInProgress { get; }
	}
}
