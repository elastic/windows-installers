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

namespace Elastic.Installer.Domain.Service
{
	public partial class ElasticsearchService : ServiceBase
	{
		public ElasticsearchNode Node { get; set; }

		public ElasticsearchService(IEnumerable<string> arguments)
		{
			InitializeComponent();
			this.Node = new ElasticsearchNode(arguments);
		}

		protected override void OnStart(string[] args) => this.Node.Start();

		protected override void OnStop() => this.Node.Stop();

		public void StartInteractive() => this.OnStart(null);
		public void StopInteractive() => this.OnStop();
	}
}
