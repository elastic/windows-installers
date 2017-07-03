using Semver;

namespace Elastic.Installer.Domain.Configuration.Wix
{
	public class VersionConfiguration
	{
		public SemVersion ExistingVersion { get; }
		public SemVersion CurrentVersion { get; }

		public VersionChange VersionChange { get; } 
		public InstallationDirection InstallationDirection { get; }

		public bool AlreadyInstalled => VersionChange != VersionChange.New;
		public bool SameVersionAlreadyInstalled => VersionChange == VersionChange.Same;
		public bool HigherVersionAlreadyInstalled => InstallationDirection == InstallationDirection.Down; 

		public VersionConfiguration(IWixStateProvider wixStateProvider)
		{
			var c = this.CurrentVersion = wixStateProvider.CurrentVersion;
			var e = this.ExistingVersion = wixStateProvider.ExistingVersion;

			var v = VersionChange.New;
			var d = InstallationDirection.None;

			if (e != null)
			{
				if (c == e)
				{
					v = VersionChange.Same;
					d = InstallationDirection.None;
				}
				else if (c > e) d = InstallationDirection.Up;
				else if (c < e) d = InstallationDirection.Down;

				if (c.Major != e.Major) v = VersionChange.Major;
				else if (c.Minor != e.Minor) v = VersionChange.Minor;
				else if (c.Patch != e.Patch) v = VersionChange.Patch;
				else if (c.Prerelease != e.Prerelease) v = VersionChange.Prerelease;
			}
			this.VersionChange = v;
			this.InstallationDirection = d;
		}
	}
}