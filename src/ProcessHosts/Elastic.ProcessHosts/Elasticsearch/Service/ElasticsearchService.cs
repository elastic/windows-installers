using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elastic.ProcessHosts.Elasticsearch.Process;

namespace Elastic.ProcessHosts.Elasticsearch.Service
{
	public partial class ElasticsearchService : ProcessHosts.Service.Service
	{
		private ElasticsearchProcess _node;
		private readonly string[] _args;

		public int? LastExitCode => _node?.LastExitCode;

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
