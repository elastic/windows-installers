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

namespace Elastic.Installer.Domain.Service.Elasticsearch
{
	public partial class ElasticsearchService : Service
	{
		private ElasticsearchNode _node;

		public ElasticsearchService(IEnumerable<string> args)
		{
			InitializeComponent();
			this._node = new ElasticsearchNode(args);
		}

		public override string Name => "Elasticsearch";

		protected override void OnStart(string[] args) => this._node.Start();

		protected override void OnStop() => this._node.Stop();

		public override void WriteToConsole(ConsoleColor color, string value) => 
			ElasticsearchConsole.WriteLine(color, value);
	}
}
