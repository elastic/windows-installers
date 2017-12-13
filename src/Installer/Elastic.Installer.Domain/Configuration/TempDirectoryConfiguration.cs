using System;
using System.IO.Abstractions;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Wix.Session;

namespace Elastic.Installer.Domain.Configuration
{
	public class TempDirectoryConfiguration
	{
		private const string DirectorySuffix = "_Installation";
		private ISession Session { get; }
		private IFileSystem FileSystem { get; }
		private string ProductName { get; }
		public string TempProductInstallationDirectory { get; }
		public TempDirectoryConfiguration(ISession session, IElasticsearchEnvironmentStateProvider esState, IFileSystem fileSystem)
		{
			Session = session;
			FileSystem = fileSystem;
			ProductName = this.Session.ProductName;
			TempProductInstallationDirectory = this.FileSystem.Path.Combine(esState.TempDirectoryVariable, ProductName + DirectorySuffix);
		}

		public void CleanUp()
		{
			if (!this.FileSystem.Directory.Exists(this.TempProductInstallationDirectory)) return;
			try
			{
				this.FileSystem.Directory.Delete(this.TempProductInstallationDirectory, true);
			}
			catch (Exception e)
			{
				// log, but continue.
				this.Session.Log($"Exception deleting {this.TempProductInstallationDirectory}: {e}");
			}
		}

	}
}
