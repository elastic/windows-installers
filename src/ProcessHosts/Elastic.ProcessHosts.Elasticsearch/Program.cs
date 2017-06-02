using System;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.Extensions;
using Elastic.ProcessHosts.Elasticsearch.Service;

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
			if (args.Length == 1 && args[0] == "--debug-env")
			{
				WriteEnvironmentDebugInformation();
				return 0;
			}
			if (args.Length == 1 && args[0] == "--java-home")
			{
				Console.WriteLine(JavaConfiguration.Default.JavaHomeCanonical);
				return 0;
			}
			if (JavaConfiguration.Default.Using32BitJava && Environment.UserInteractive)
				Console.WriteLine("You are using a 32bit version this may cause the JVM to start, consider installing a 64bit JVM");
			
			ElasticsearchService service = null;
			try
			{
				service = new ElasticsearchService(args);
				if (Environment.UserInteractive)
					Console.Title = $"Elasticsearch {AssemblyVersionInformation.AssemblyFileVersion}";
				service.Run();
				var exitCode = service.LastExitCode ?? 0;
				return exitCode;
			}
			catch (Exception e)
			{
				var exitCode = service?.LastExitCode ?? 1;
				if (Environment.UserInteractive)
					e.ToConsole("An exception occurred while trying to start elasticsearch.");
				e.ToEventLog("Elasticsearch");
				return exitCode;
			}
			finally
			{
				service?.Dispose();
				Console.ResetColor();
			}
		}


		private static void WriteEnvironmentDebugInformation()
		{
			Console.WriteLine("-------------");
			Console.WriteLine("Elasticsearch");
			Console.WriteLine("-------------");
			Console.WriteLine(ElasticsearchEnvironmentConfiguration.Default);
			Console.WriteLine("-------------");
			Console.WriteLine("Java");
			Console.WriteLine("-------------");
			Console.WriteLine(JavaConfiguration.Default);
		}
	}
}