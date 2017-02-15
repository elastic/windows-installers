using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Kibana.Process;

namespace Elastic.Installer.Domain.Kibana.Service
{
	public partial class KibanaService : Domain.Service.Service
	{
		private readonly KibanaProcess _process;

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
