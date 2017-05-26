﻿using Elastic.Installer.Domain.Configuration.Wix;
using Semver;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockWixStateProvider : IWixStateProvider
	{
		public MockWixStateProvider()
		{
			this.CurrentVersion = "5.0.0-alpha5";
		}

		public SemVersion ExistingVersion { get; set; }

		public SemVersion CurrentVersion { get; set; }
	}
}
