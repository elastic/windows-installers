using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Elastic.Installer.Domain.Service;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Service.Elasticsearch;

namespace Elastic.Installer.Domain.Shared.Configuration
{
	public interface IServiceStateProvider
	{
		bool SeesService { get; }
		bool Running { get; }

		void RunTimeInstall(ServiceConfiguration config);
		void StartAndWaitForRunning(TimeSpan timeToWait, int totalTicks);
		void RunTimeUninstall(ServiceConfiguration config);
		void StopIfRunning(TimeSpan timeToWait);
	}

	public class ServiceStateProvider : IServiceStateProvider
	{
		public static ServiceStateProvider FromSession(ISession session, string serviceName) => new ServiceStateProvider(session, serviceName);

		private readonly ISession _session;
		private readonly string _serviceName;

		public ServiceStateProvider(ISession session, string serviceName)
		{
			_session = session;
			_serviceName = serviceName;
		}

		public bool SeesService => ServiceController.GetServices().Any(s => s.ServiceName.Equals(_serviceName));

		public bool Running
		{
			get
			{
				if (!SeesService) return false;
				using (var service = new ServiceController(_serviceName))
					return service.Status == ServiceControllerStatus.Running;
			}
		}

		public void RunTimeInstall(ServiceConfiguration config)
		{
			if (this.SeesService) ServiceInstallationManager.RuntimeUninstall(config);
			ServiceInstallationManager.RuntimeInstall(config);
		}

		public void RunTimeUninstall(ServiceConfiguration config)
		{
			ServiceInstallationManager.RuntimeUninstall(config);
		}
		
		public void StopIfRunning(TimeSpan timeToWait)
		{
			if (!SeesService) return;
			using (var service = new ServiceController(_serviceName))
			{
				if (service.Status == ServiceControllerStatus.Running)
				{
					service.Stop();
					service.WaitForStatus(ServiceControllerStatus.Stopped, timeToWait);
				}
			}
		}

		public void StartAndWaitForRunning(TimeSpan timeToWait, int totalTicks)
		{
			if (!SeesService) return;
			using (var service = new ServiceController(_serviceName))
			{
				service.Start();
				service.Refresh();

				var timeout = TimeSpan.FromSeconds(60);
				var sleepyTime = 250;
				var ticksPerSleep = (int)Math.Floor(totalTicks / (timeout.TotalMilliseconds / sleepyTime));
				var tickCount = 0;
				var utcNow = DateTime.UtcNow;

				while (service.Status != ServiceControllerStatus.Running)
				{
					if (DateTime.UtcNow - utcNow > timeout)
						throw new System.ServiceProcess.TimeoutException("Time out has expired and the operation has not been completed.");

					Thread.Sleep(sleepyTime);
					service.Refresh();
					_session.SendProgress(ticksPerSleep, "waiting to start");
					tickCount += ticksPerSleep;
				}

				_session.SendProgress(totalTicks - tickCount, "started");
			}
		}
	}

	public class NoopServiceStateProvider : IServiceStateProvider
	{
		public bool SeesService { get; set; }
		public bool Running { get; set; }

		public ServiceConfiguration SeenServiceConfig { get; private set; }

		public void RunTimeInstall(ServiceConfiguration config)
		{
			this.SeenServiceConfig = config;
		}
		public void RunTimeUninstall(ServiceConfiguration config)
		{
			this.SeenServiceConfig = config;
		}

		public void StopIfRunning(TimeSpan timeToWait) { }
		public void StartAndWaitForRunning(TimeSpan timeToWait, int totalTicks) { }
	}
}
