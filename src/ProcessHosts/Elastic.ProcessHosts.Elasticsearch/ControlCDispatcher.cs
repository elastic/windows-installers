using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using static System.Diagnostics.Process;

namespace Elastic.ProcessHosts.Elasticsearch
{
	[SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
	internal static class ControlCDispatcher
	{
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

		public static bool Send(int processId)
		{
			if (!ProcessIdIsElasticsearch(processId)) return false;

			FreeConsole();
			if (!AttachConsole((uint) processId)) return false;
			SetConsoleCtrlHandler(null, true);
			try
			{
				if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
					return false;
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
				var p = GetProcessById(processId);
				return p.ProcessName.Equals("java", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}
	}
}