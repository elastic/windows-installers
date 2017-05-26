using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Kibana;

namespace Elastic.InstallerHosts.Kibana.Tasks
{
	public class StopServiceTask : KibanaInstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public StopServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Kibana");
		}
		public StopServiceTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			var seesService = this.ServiceStateProvider.SeesService;
			this.Session.Log($"Trying to execute StopServiceTask seeing service: " + seesService);
			if (!seesService) return true;
			var totalTicks = 1000;
			this.Session.SendActionStart(totalTicks, ActionName, "Stopping existing Kibana service");
			this.ServiceStateProvider.StopIfRunning(TimeSpan.FromSeconds(60));
			this.Session.SendProgress(1000, "Kibana service stopped");
			return true;
		}
	}
}
