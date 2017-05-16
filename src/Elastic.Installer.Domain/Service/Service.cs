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
		public abstract void WriteToConsole(ConsoleColor color, string value);

		[DllImport("Kernel32")]
		public static extern bool SetConsoleCtrlHandler(ConsoleCtrlHandler Handler, bool Add);
		public delegate bool ConsoleCtrlHandler(int ctrlType);
		private static ConsoleCtrlHandler _consoleCtrlHandler;

		public void Run()
		{
			if (Environment.UserInteractive)
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
			else
			{
				Run(this);
			}
		}

		private void WarnIfAlreadyRunningAsAService()
		{
			var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName.Equals(this.Name));
			if (service != null && service.Status != ServiceControllerStatus.Stopped)
			{
				var status = Enum.GetName(typeof(ServiceControllerStatus), service.Status);
				ElasticsearchConsole.WriteLine(ConsoleColor.Blue,
					$"{this.Name} is already running as a service and currently: {status}.");
			}
		}
	}
}
