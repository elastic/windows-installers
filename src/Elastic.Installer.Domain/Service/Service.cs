using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Service
{
	public interface IService : IDisposable
	{
		string Name { get; }
		void StartInteractive();
		void StopInteractive();
		void Run();
		void WriteToConsole(ConsoleColor color, string value);
	}

	public abstract class Service : ServiceBase, IService
	{
		public abstract string Name { get; }
		public virtual void StartInteractive() => this.OnStart(null);
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
					this.WriteToConsole(ConsoleColor.Red, $"Stopping {this.Name}...");
					this.StopInteractive();
					handle.Set();
					return false;
				});
				SetConsoleCtrlHandler(_consoleCtrlHandler, true);
				this.StartInteractive();
				handle.WaitOne();
			}
		}

		private void WarnIfAlreadyRunningAsAService()
		{
			var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(this.Name));
			if (service == null || service.Status == ServiceControllerStatus.Stopped) return;

			var status = Enum.GetName(typeof(ServiceControllerStatus), service.Status);
			this.WriteToConsole(ConsoleColor.Blue, $"{this.Name} is already running as a service and currently: {status}.");
		}
	}
}
