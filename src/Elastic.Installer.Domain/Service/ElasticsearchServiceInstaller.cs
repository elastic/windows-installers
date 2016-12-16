using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Service
{
	[RunInstaller(true)]
	public partial class ElasticsearchServiceInstaller : System.Configuration.Install.Installer
	{
		public ElasticsearchServiceConfiguration Configuration { get; set; }

		public ElasticsearchServiceInstaller(ElasticsearchServiceConfiguration configuration)
		{
			Configuration = configuration;			
		}

		public static void RuntimeInstall(ElasticsearchServiceConfiguration configuration)
		{
			string path = "/assemblypath=" + configuration.ExeLocation;

			using (var ti = new TransactedInstaller())
			{
				ti.Installers.Add(new ElasticsearchServiceInstaller(configuration));
				ti.Context = new InstallContext(null, new[] { path });
				ti.Install(new Hashtable());
			}
		}

		public static void RuntimeUninstall(ElasticsearchServiceConfiguration configuration)
		{
			string path = "/assemblypath=" + configuration.ExeLocation;

			using (var ti = new TransactedInstaller())
			{
				ti.Installers.Add(new ElasticsearchServiceInstaller(configuration));
				ti.Context = new InstallContext(null, new[] { path });
				ti.Uninstall(null);
			}
		}

		public override void Install(IDictionary stateSaver)
		{
			ConfigureInstallers();
			base.Install(stateSaver);

			if (!string.IsNullOrWhiteSpace(Configuration.EventLogSource))
			{
				if (!EventLog.SourceExists(Configuration.EventLogSource))
				{
					EventLog.CreateEventSource(Configuration.EventLogSource, "Application");
				}
			}
		}

		public override void Uninstall(IDictionary savedState)
		{
			ConfigureInstallers();
			base.Uninstall(savedState);

			if (!string.IsNullOrWhiteSpace(Configuration.EventLogSource))
				if (EventLog.SourceExists(Configuration.EventLogSource))
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

			if (result.Account == ServiceAccount.User)
			{
				result.Username = Configuration.UserName;
				result.Password = Configuration.Password;
			}

			return result;
		}

		private ServiceInstaller ConfigureServiceInstaller()
		{
			var result = new ServiceInstaller
			{
				ServiceName = Configuration.Name,
				DisplayName = Configuration.DisplayName,
				Description = Configuration.Description,
				StartType = Configuration.StartMode,
			};

			return result;
		}
	}
}
