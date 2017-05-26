using System;
using Elastic.Configuration.Extensions;
using Elastic.ProcessHosts.Kibana.Service;

namespace Elastic.ProcessHosts.Kibana
{
	class Program
	{
		static void Main(string[] args)
		{
			try
			{
				using (var service = new KibanaService(args)) service.Run();
			}
			catch (Exception e)
			{
				e.ToEventLog("Kibana");
				if (Environment.UserInteractive)
					e.ToConsole("An exception occurred while trying to start the service.");
			}
		}
	}
}
