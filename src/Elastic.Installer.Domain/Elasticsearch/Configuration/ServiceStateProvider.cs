using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using Elastic.Installer.Domain.Service;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration
{
	public interface IServiceStateProvider
	{
		bool SeesService { get; }
		bool Running { get; }

		void RunTimeInstall(ElasticsearchServiceConfiguration config);
		void StartAndWaitForRunning(TimeSpan timeToWait, int totalTicks);
		void RunTimeUninstall(ElasticsearchServiceConfiguration elasticsearchServiceConfiguration);
		void StopIfRunning(TimeSpan timeToWait);
	}

	public class ServiceStateProvider : IServiceStateProvider
	{
		public static ServiceStateProvider FromSession(ISession session) => new ServiceStateProvider(session);
		private static readonly string ServiceName = "Elasticsearch";

		private readonly ISession _session;

		public ServiceStateProvider(ISession session)
		{
			_session = session;
		}

		public bool SeesService => ServiceController.GetServices().Any(s => s.ServiceName.Equals(ServiceName));

		public bool Running
		{
			get
			{
				if (!SeesService) return false;
				using (var service = new ServiceController(ServiceName))
					return service.Status == ServiceControllerStatus.Running;
			}
		}

		public void RunTimeInstall(ElasticsearchServiceConfiguration config)
		{
			if (this.SeesService) ElasticsearchServiceInstaller.RuntimeUninstall(config);
			ElasticsearchServiceInstaller.RuntimeInstall(config);
		}

		public void RunTimeUninstall(ElasticsearchServiceConfiguration config)
		{
			ElasticsearchServiceInstaller.RuntimeUninstall(config);
		}
		
		public void StopIfRunning(TimeSpan timeToWait)
		{
			if (!SeesService) return;
			using (var service = new ServiceController(ServiceName))
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
			using (var service = new ServiceController(ServiceName))
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

		public ElasticsearchServiceConfiguration SeenServiceConfig { get; private set; }

		public void RunTimeInstall(ElasticsearchServiceConfiguration config)
		{
			this.SeenServiceConfig = config;
		}
		public void RunTimeUninstall(ElasticsearchServiceConfiguration config)
		{
			this.SeenServiceConfig = config;
		}

		public void StopIfRunning(TimeSpan timeToWait) { }
		public void StartAndWaitForRunning(TimeSpan timeToWait, int totalTicks) { }
	}
}
