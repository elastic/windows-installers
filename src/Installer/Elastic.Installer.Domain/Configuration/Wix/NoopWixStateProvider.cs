using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class NoopWixStateProvider : IWixStateProvider
	{
		public SemVersion PreviousVersion { get; set; }

		public SemVersion InstallerVersion { get; set; } = "0.0.1-ignored";
	}
}