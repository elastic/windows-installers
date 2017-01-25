using Elastic.Installer.Domain.Extensions;
using Elastic.Installer.Domain.Service.Elasticsearch;
using System;

namespace Elastic.Installer.Elasticsearch.Process
{
	static class Program
	{
		static void Main(string[] args)
		{
			try
			{
				using (var service = new ElasticsearchService(args)) service.Run();
			}
			catch (Exception e)
			{
				e.ToEventLog("Elasticsearch");
				if (Environment.UserInteractive)
					e.ToConsole("An exception occurred while trying to start the service.");
			}
		}
	}
}
