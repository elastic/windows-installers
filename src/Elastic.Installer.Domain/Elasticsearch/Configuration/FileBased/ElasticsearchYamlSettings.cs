using System.Collections.Generic;
using System.ComponentModel;
using SharpYaml.Serialization;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased
{
	public class ElasticsearchYamlSettings : Dictionary<string, object>
	{
		[YamlMember("cluster.name", SerializeMemberMode.Content)]
		[DefaultValue(null)]
		public string ClusterName { get; set; }

		[YamlMember("node.name")]
		[DefaultValue(null)]
		public string NodeName { get; set; }

		[YamlMember("node.master")]
		[DefaultValue(null)]
		public bool? MasterNode { get; set; }

		[YamlMember("node.data")]
		[DefaultValue(null)]
		public bool? DataNode { get; set; }

		[YamlMember("node.ingest")]
		[DefaultValue(null)]
		public bool? IngestNode { get; set; }

		[YamlMember("bootstrap.memory_lock")]
		[DefaultValue(null)]
		public bool? MemoryLock { get; set; }

		[YamlMember("path.data")]
		[DefaultValue(null)]
		public string DataPath { get; set; }

		[YamlMember("path.logs")]
		[DefaultValue(null)]
		public string LogsPath { get; set; }

		[YamlMember("discovery.zen.ping.unicast.hosts")]
		[DefaultValue(null)]
		public List<string> UnicastHosts { get; set; }

		[YamlMember("node.max_local_storage_nodes")]
		[DefaultValue(null)]
		public int? MaxLocalStorageNodes { get; set; }

		[YamlMember("discovery.zen.minimum_master_nodes")]
		[DefaultValue(null)]
		public int? MinimumMasterNodes { get; set; }

		[YamlMember("network.host")]
		[DefaultValue(null)]
		public string NetworkHost { get; set; }

		[YamlMember("http.port")]
		[DefaultValue(null)]
		public string HttpPortString { get; set; }

		public int? HttpPort
		{
			get
			{
				int port;
				if (int.TryParse(HttpPortString, out port)) return port;
				return null;
			}
		}
		
		[YamlMember("transport.tcp.port")]
		[DefaultValue(null)]
		public string TransportTcpPortString { get; set; }
		
		public int? TransportTcpPort
		{
			get
			{
				int port;
				if (int.TryParse(TransportTcpPortString, out port)) return port;
				return null;
			}
		}
	}
}