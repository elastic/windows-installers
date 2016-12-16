using System;
using Semver;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased
{
	public class NoopWixStateProvider : IWixStateProvider
	{
		public SemVersion CurrentVersion => null;
		public SemVersion ExistingVersion => null;
	}
}
