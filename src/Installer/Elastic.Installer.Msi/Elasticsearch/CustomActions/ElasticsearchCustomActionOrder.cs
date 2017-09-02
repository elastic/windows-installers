namespace Elastic.Installer.Msi.Elasticsearch.CustomActions
{
	public enum ElasticsearchCustomActionOrder
	{
		// Immediate actions
		LogAllTheThings = 1,

		// Deferred actions
		SetPreconditions = 2,
		InstallPreserveInstall = 3,
		InstallStopServiceAction = 3,
		InstallEnvironment = 4,
		InstallDirectories = 5,
		InstallConfiguration = 6,
		InstallJvmOptions = 7,
		InstallPlugins = 8,
		InstallService = 9,
		InstallStartService = 10,

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
