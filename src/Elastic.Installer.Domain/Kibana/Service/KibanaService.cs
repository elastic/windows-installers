using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Process;
using Elastic.Installer.Domain.Kibana.Process;

namespace Elastic.Installer.Domain.Service.Kibana
{
	public partial class KibanaService : Service
	{
		private KibanaProcess _process;

		public KibanaService(IEnumerable<string> args)
		{
			InitializeComponent();
			this._process = new KibanaProcess(args);
		}

		public override string Name => "Kibana";

		protected override void OnStart(string[] args) => this._process.Start();

		protected override void OnStop() => this._process.Stop();

		public override void WriteToConsole(ConsoleColor color, string value) => Console.WriteLine(value);
	}
}
