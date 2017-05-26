using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.ServiceProcess;

namespace Elastic.Installer.Domain.Configuration.Service
{
	[RunInstaller(true)]
	public partial class ServiceInstallationManager : System.Configuration.Install.Installer
	{
		private ServiceConfiguration Configuration { get; }

		public ServiceInstallationManager(ServiceConfiguration configuration)
		{
			Configuration = configuration;			
		}

		public static void RuntimeInstall(ServiceConfiguration configuration)
		{
			var path = "/assemblypath=" + configuration.ExeLocation;

			using (var ti = new TransactedInstaller())
			{
				ti.Installers.Add(new ServiceInstallationManager(configuration));
				ti.Context = new InstallContext(null, new[] { path });
				ti.Install(new Hashtable());
			}
		}

		public static void RuntimeUninstall(ServiceConfiguration configuration)
		{
			var path = "/assemblypath=" + configuration.ExeLocation;

			using (var ti = new TransactedInstaller())
			{
				ti.Installers.Add(new ServiceInstallationManager(configuration));
				ti.Context = new InstallContext(null, new[] { path });
				ti.Uninstall(null);
			}
		}

		public override void Install(IDictionary stateSaver)
		{
			ConfigureInstallers();
			base.Install(stateSaver);

			if (string.IsNullOrWhiteSpace(Configuration.EventLogSource)) return;
			if (EventLog.SourceExists(Configuration.EventLogSource)) return;
			EventLog.CreateEventSource(Configuration.EventLogSource, "Application");
		}

		public override void Uninstall(IDictionary savedState)
		{
			ConfigureInstallers();
			base.Uninstall(savedState);

			if (string.IsNullOrWhiteSpace(Configuration.EventLogSource)) return;
			if (!EventLog.SourceExists(Configuration.EventLogSource)) return;
			EventLog.DeleteEventSource(Configuration.EventLogSource);
		}

		public override void Rollback(IDictionary savedState)
		{
			ConfigureInstallers();
			base.Rollback(savedState);
		}

		private void ConfigureInstallers()
		{
			Installers.Add(ConfigureProcessInstaller());
			Installers.Add(ConfigureServiceInstaller());
		}

		private ServiceProcessInstaller ConfigureProcessInstaller()
		{
			var result = new ServiceProcessInstaller
			{
				Account = Configuration.ServiceAccount
			};

			if (result.Account != ServiceAccount.User)
				return result;
			result.Username = Configuration.UserName;
			result.Password = Configuration.Password;

			return result;
		}

		private ServiceInstaller ConfigureServiceInstaller() => new ServiceInstaller
		{
			ServiceName = Configuration.Name,
			DisplayName = Configuration.DisplayName,
			Description = Configuration.Description,
			StartType = Configuration.StartMode,
		};
	}
}
