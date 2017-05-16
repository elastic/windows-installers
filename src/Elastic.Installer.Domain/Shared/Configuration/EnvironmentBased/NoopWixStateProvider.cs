using System;
using Semver;

namespace Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased
{
	public class NoopWixStateProvider : IWixStateProvider
	{
		public SemVersion CurrentVersion => null;
		public SemVersion ExistingVersion => null;
	}
}
