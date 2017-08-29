namespace Elastic.Installer.Msi.Elasticsearch.CustomActions
{
	public enum ElasticsearchCustomActionOrder
	{
		// Immediate actions
		LogAllTheThings = 1,

		// Deferred actions
		SetPreconditions = 2,
		InstallEnvironment = 3,	
		InstallDirectories = 4,		
		InstallConfiguration = 5,	
		InstallJvmOptions = 6,	
		InstallPlugins = 7,	
		InstallService = 8,
		InstallStartService = 9,

		// Rollback actions are played in reverse order
		RollbackEnvironment = 1,
		RollbackDirectories = 2,
		RollbackService = 3,
	
		// Uninstall actions
		UninstallService = 1,
		UninstallPlugins = 2,
		UninstallDirectories = 3,
		UninstallEnvironment = 4
	}
}
