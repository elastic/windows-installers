using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using SharpYaml.Serialization;

namespace Elastic.Configuration.FileBased.Yaml
{
	public class ElasticsearchYamlSettings : Dictionary<string, object>, IYamlSettings
	{
		private List<string> _nodeRoles;

		[YamlMember("cluster.name", SerializeMemberMode.Content)]
		[DefaultValue(null)]
		public string ClusterName { get; set; }

		[YamlMember("node.name")]
		[DefaultValue(null)]
		public string NodeName { get; set; }

		[YamlMember("node.master")]
		[DefaultValue(null)]
		public bool? MasterNode
		{
			get => null;
			set
			{
				this._masterNode = value;
				SetNodeRole("master", value);
			}
		}

		[YamlMember("node.data")]
		[DefaultValue(null)]
		public bool? DataNode
		{
			get => null;
			set
			{
				this._dataNode = value;
				SetNodeRole("data", value);
			}
		}

		[YamlMember("node.ingest")]
		[DefaultValue(null)]
		public bool? IngestNode
		{
			get => null;
			set
			{
				this._ingestNode = value;
				SetNodeRole("ingest", value);
			}
		}

		public bool? HasNodeRole(string role)
		{
			if (role == "data" && _dataNode.HasValue) return _dataNode;
			if (role == "ingest" && _ingestNode.HasValue) return _ingestNode;
			if (role == "master" && _masterNode.HasValue) return _masterNode;
			return NodeRoles?.Any(r => r != null && r.Equals(role, StringComparison.InvariantCultureIgnoreCase));
		}

		public void SetNodeRole(string role, bool? include)
		{
			if (_nodeRoles == null) _nodeRoles = new List<string>();
			if (include.HasValue && include.Value) _nodeRoles.Add(role.ToLowerInvariant());
			else _nodeRoles.Remove(role.ToLowerInvariant());
			if (_nodeRoles.Count == 0) _nodeRoles = null;
			if (_nodeRoles != null) _nodeRoles = new HashSet<string>(_nodeRoles).ToList();
		}

		private HashSet<string> _allRoles = new HashSet<string>(new [] {"ingest", "data", "master"});
		private bool? _masterNode;
		private bool? _dataNode;
		private bool? _ingestNode;

		[YamlMember("node.roles")]
		[DefaultValue(null)]
		public List<string> NodeRoles
		{
			get
			{
				if (_nodeRoles == null) return null;
				//if all roles are set prefer not setting roles at all and rely on the defaults
				return new HashSet<string>(_nodeRoles).SetEquals(_allRoles) ? null : _nodeRoles;
			}
			set => _nodeRoles = value;
		}


		[YamlMember("bootstrap.memory_lock")]
		[DefaultValue(null)]
		public bool? MemoryLock { get; set; }

		[YamlMember("path.data")]
		[DefaultValue(null)]
		public string DataPath { get; set; }

		[YamlMember("path.logs")]
		[DefaultValue(null)]
		public string LogsPath { get; set; }

		// deprecated in 7.0 we will fallback to reading from this but never write it out on version >= 7.0
		[YamlMember("discovery.zen.ping.unicast.hosts")]
		[DefaultValue(null)]
		public List<string> UnicastHosts { get; set; }

		[YamlMember("discovery.seed_hosts")]
		[DefaultValue(null)]
		public List<string> SeedHosts { get; set; }
		
		[YamlMember("cluster.initial_master_nodes")]
		[DefaultValue(null)]
		public List<string> InitialMasterNodes { get; set; }

		[YamlMember("node.max_local_storage_nodes")]
		[DefaultValue(null)]
		public int? MaxLocalStorageNodes { get; set; }

		/// deprecated, will read but won't emit in >= 7.0
		[YamlMember("discovery.zen.minimum_master_nodes")]
		[DefaultValue(null)]
		public int? MinimumMasterNodes { get; set; }

		[YamlMember("network.host")]
		[DefaultValue(null)]
		public string NetworkHost { get; set; }

		[YamlMember("http.port")]
		[DefaultValue(null)]
		public string HttpPortString { get; set; }

		public int? HttpPort => int.TryParse(HttpPortString, out int port) ? port : (int?)null;

		[YamlMember("transport.tcp.port")]
		[DefaultValue(null)]
		public string TransportTcpPortString { get; set; }
		
		public int? TransportTcpPort => int.TryParse(TransportTcpPortString, out int port) ? port : (int?)null;
		
		[YamlMember("xpack.license.self_generated.type")]
		[DefaultValue(null)]
		public string XPackLicenseSelfGeneratedType { get; set; }
		
		[YamlMember("xpack.security.enabled")]
		[DefaultValue(null)]
		public bool? XPackSecurityEnabled { get; set; }
	}
}