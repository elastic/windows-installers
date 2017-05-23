using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Service
{
	public interface IService : IDisposable
	{
		string Name { get; }
		void StartInteractive(ManualResetEvent handle);
		void StopInteractive();
		void Run();
		void WriteToConsole(ConsoleColor color, string value);
	}

	public abstract class Service : ServiceBase, IService
	{
		public abstract string Name { get; }
		public virtual void StartInteractive(ManualResetEvent handle) => this.OnStart(null);
		public virtual void StopInteractive() => this.OnStop();
		public void WriteToConsole(ConsoleColor color, string value)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(value);
			Console.ResetColor();
		}

		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler Handler, bool Add);
		public delegate bool ConsoleCtrlHandler(int ctrlType);
		private static ConsoleCtrlHandler _consoleCtrlHandler;

		public void Run()
		{
			if (!Environment.UserInteractive)
				Run(this);
			else
			{
				WarnIfAlreadyRunningAsAService();

				var handle = new ManualResetEvent(false);
				_consoleCtrlHandler += new ConsoleCtrlHandler(c =>
				{
					this.WriteToConsole(ConsoleColor.Cyan, $"Stop requested, stopping {this.Name}...");
					this.StopInteractive();
					handle.Set();
					return true;
				});
				SetConsoleCtrlHandler(_consoleCtrlHandler, true);
				this.StartInteractive(handle);
				handle.WaitOne();
			}
		}

		private void WarnIfAlreadyRunningAsAService()
		{
			var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(this.Name));
			if (service == null || service.Status == ServiceControllerStatus.Stopped) return;

			var status = Enum.GetName(typeof(ServiceControllerStatus), service.Status);

			throw new StartupException($"{this.Name} is already running as a service and currently: {status}.");
		}
	}
}
