using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Elasticsearch.Process;
using Elastic.Installer.Domain.Process;

namespace Elastic.Installer.Domain.Service.Elasticsearch
{
	public partial class ElasticsearchService : Service
	{
		private ElasticsearchProcess _node;
		private readonly string[] _args;

		public ElasticsearchService(IEnumerable<string> args)
		{
			InitializeComponent();
			this._args = args?.ToArray();
		}

		public override string Name => "Elasticsearch";

		protected override void OnStart(string[] args)
		{
			//todo merge args?
			this._node = new ElasticsearchProcess(null, this._args);
			this._node.Start();
		}

		public override void StartInteractive(ManualResetEvent handle)
		{
			this._node = new ElasticsearchProcess(handle, this._args);
			this._node.Start();
		}

		protected override void OnStop() {
			this._node?.Stop();
			this._node?.Dispose();
			this._node = null;
		}
	}
}
