using Elastic.Installer.Domain.Extensions;
using Elastic.Installer.Domain.Service.Elasticsearch;
using System;
using System.Runtime.InteropServices;

namespace Elastic.Installer.Elasticsearch.Process
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			if (args.Length == 2 && args[0] == "--clean-shutdown")
			{
				var sent = SendCtrlCTo(int.Parse(args[1]));
				return sent ? 0 : 1;
			}
			var service = new ElasticsearchService(args);
			try
			{
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
			}
		}

		private const int CTRL_C_EVENT = 0;
		[DllImport("kernel32.dll")]
		private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AttachConsole(uint dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
		private static extern bool FreeConsole();
		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
		private delegate bool ConsoleCtrlDelegate(uint CtrlType);

		private static bool SendCtrlCTo(int processId)
		{
			if (!ProcessIdIsElasticsearch(processId)) return false;

			FreeConsole();
			if (!AttachConsole((uint) processId)) return false;
			SetConsoleCtrlHandler(null, true);
			try
			{
				if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
					return false;
				//p.WaitForExit();
			}
			finally
			{
				FreeConsole();
				SetConsoleCtrlHandler(null, false);
			}
			return true;
		}



		private static bool ProcessIdIsElasticsearch(int processId)
		{
			try
			{
				var p = System.Diagnostics.Process.GetProcessById(processId);
				return p.ProcessName.Equals("java", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}

		}
	}
}
