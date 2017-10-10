namespace Elastic.Installer.Msi.Elasticsearch.CustomActions
{
	public enum ElasticsearchCustomActionOrder
	{
		// Immediate actions
		LogAllTheThings = 1,
		BootstrapPasswordProperty = 2,

		// Deferred actions
		SetPreconditions = 2,
		InstallPreserveInstall = 3,
		InstallStopServiceAction = 3,
		InstallEnvironment = 4,
		InstallDirectories = 5,
		InstallConfiguration = 6,
		InstallJvmOptions = 7,
		InstallPlugins = 8,
		BootstrapPassword = 9,
		InstallService = 10,
		InstallStartService = 11,

		// Rollback actions are played in reverse order
		RollbackEnvironment = 1,
		RollbackDirectories = 2,
		RollbackServiceStart = 3,
		RollbackServiceInstall = 4,
	
		// Uninstall actions
		UninstallService = 1,
		UninstallPlugins = 2,
		UninstallDirectories = 3,
		UninstallEnvironment = 4,

		// Commit actons
		CleanupInstall = 1,
	}
}
