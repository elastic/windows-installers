using Elastic.Installer.Domain.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Kibana.Service;

namespace Elastic.Installer.Kibana.Process
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
