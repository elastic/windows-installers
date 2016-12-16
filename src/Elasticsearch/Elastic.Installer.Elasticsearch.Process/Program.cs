using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Extensions;
using Elastic.Installer.Domain.Service;


namespace Elastic.Installer.Elasticsearch.Process
{
	static class Program
	{
		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler Handler, bool Add);
		public delegate bool ConsoleCtrlHandler(int ctrlType);
		private static ConsoleCtrlHandler _consoleCtrlHandler;

		static void Main(string[] args)
		{
			try
			{
				using (var service = new ElasticsearchService(args))
				{
					if (Environment.UserInteractive)
					{
						WarnRunningService();

						var handle = new ManualResetEvent(false);
						_consoleCtrlHandler += new ConsoleCtrlHandler(c =>
						{
							ElasticsearchConsole.WriteLine(ConsoleColor.Red, "Stopping Elasticsearch...");
							service.StopInteractive();
							handle.Set();
							return false;
						});
						SetConsoleCtrlHandler(_consoleCtrlHandler, true);
						service.StartInteractive();
						handle.WaitOne();
					}
					else
					{
						ServiceBase.Run(service);
					}
				}
			}
			catch (Exception e)
			{
				e.ToEventLog("Elasticsearch");
				if (Environment.UserInteractive)
					e.ToConsole("An exception occurred in Main()");
			}
		}

		private static void WarnRunningService()
		{
			var elasticsearchService = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals("Elasticsearch"));
			if (elasticsearchService != null && elasticsearchService.Status != ServiceControllerStatus.Stopped)
			{
				var status = Enum.GetName(typeof(ServiceControllerStatus), elasticsearchService.Status);
				ElasticsearchConsole.WriteLine(ConsoleColor.Blue,
					$"Elasticsearch already running as a service and currently: {status}. Elasticsearch might not be able to start when not passed a different clustername.");
			}
		}
	}
}
