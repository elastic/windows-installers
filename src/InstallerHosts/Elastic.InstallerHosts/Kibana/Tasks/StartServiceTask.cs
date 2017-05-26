using System;
using System.IO.Abstractions;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Kibana;

namespace Elastic.InstallerHosts.Kibana.Tasks
{
	public class StartServiceTask : KibanaInstallationTask
	{
		private IServiceStateProvider ServiceStateProvider { get; }

		public StartServiceTask(string[] args, ISession session) : base(args, session)
		{
			this.ServiceStateProvider = new ServiceStateProvider(session, "Kibana");
		}

		public StartServiceTask(KibanaInstallationModel model, ISession session, IFileSystem fileSystem, IServiceStateProvider serviceConfig) 
			: base(model, session, fileSystem)
		{
			this.ServiceStateProvider = serviceConfig;
		}

		protected override bool ExecuteTask()
		{
			this.Session.Log("Executing start service!");
			if (!this.InstallationModel.ServiceModel.StartAfterInstall)
				return true;
			var seesService = this.ServiceStateProvider.SeesService;
			this.Session.Log($"Trying to execute StartServiceTask seeing service: {seesService}");
			if (!seesService) return true;
			var totalTicks = 1000;
			this.Session.SendActionStart(totalTicks, ActionName, "Starting Kibana service");
			this.ServiceStateProvider.StartAndWaitForRunning(TimeSpan.FromSeconds(120), 2000);
			this.Session.SendProgress(1000, "Kibana service started");
			return true;
		}
	}
}
