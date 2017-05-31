using System;
using System.Reflection;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.Extensions;
using Elastic.ProcessHosts.Elasticsearch.Service;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Elasticsearch
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 2 && args[0] == "--clean-shutdown")
			{
				var sent = ControlCDispatcher.Send(int.Parse(args[1]));
				return sent ? 0 : 1;
			}
			var service = new ElasticsearchService(args);
			try
			{
				Console.Title = $"Elasticsearch {AssemblyVersionInformation.AssemblyFileVersion}";
				service.Run();
				var exitCode = service.LastExitCode ?? 0;
				return exitCode;
			}
			catch (Exception e)
			{
				var exitCode = service.LastExitCode ?? 1;
				if (Environment.UserInteractive)
					e.ToConsole("An exception occurred while trying to start elasticsearch.");
				e.ToEventLog("Elasticsearch");
				return exitCode;
			}
			finally
			{
				service.Dispose();
				Console.ResetColor();
			}
		}
	}
}