using System.Collections.Generic;
using Elastic.ProcessHosts.Kibana.Process;

namespace Elastic.ProcessHosts.Kibana.Service
{
	public partial class KibanaService : ProcessHosts.Service.Service
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
	}
}
