namespace Elastic.Installer.Msi.Elasticsearch.CustomActions
{
	public enum ElasticsearchCustomActionOrder
	{
		// Immediate actions
		LogAllTheThings = 1,
		BootstrapPasswordProperty = 2,

		// Deferred actions
		SetPreconditions = 1,
		InstallPreserveInstall = 2,
		InstallDirectories = 3,
		InstallConfiguration = 4,
		InstallJvmOptions = 5,
		InstallPlugins = 6,
		BootstrapPassword = 7,
		SetupXPackPasswords = 8,

		// Rollback actions are played in reverse order
		RollbackDirectories = 1,
	
		// Uninstall actions
		UninstallPlugins = 1,
		UninstallDirectories = 2,

		// Commit actons
		CleanupInstall = 1,
	}
}
