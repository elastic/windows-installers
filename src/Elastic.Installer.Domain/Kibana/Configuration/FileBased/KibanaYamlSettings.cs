using Elastic.Installer.Domain.Shared.Configuration.FileBased;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Configuration.FileBased
{
	public class KibanaYamlSettings : IYamlSettings
	{
		[YamlMember("server.host")]
		[DefaultValue(null)]
		public string ServerHost { get; set; }

		[YamlMember("server.port")]
		[DefaultValue(null)]
		public int? ServerPort { get; set; }

		[YamlMember("server.basePath")]
		[DefaultValue(null)]
		public string ServerBasePath { get; set; }

		[YamlMember("server.host")]
		[DefaultValue(null)]
		public string ServerName { get; set; }

		[DefaultValue(null)]
		public string ServerDefaultRoute { get; set; }

		[YamlMember("elasticsearch.url")]
		[DefaultValue(null)]
		public string ElasticsearchUrl { get; set; }

		[YamlMember("kibana.index")]
		[DefaultValue(null)]
		public string KibanaIndex { get; set; }

		[DefaultValue(null)]
		public string ServerCert { get; set; }

		[YamlMember("server.ssl.key")]
		[DefaultValue(null)]
		public string ServerKey { get; set; }

		[YamlMember("elasticsearch.ssl.cert")]
		[DefaultValue(null)]
		public string ElasticsearchCert { get; set; }

		[YamlMember("elasticsearch.ssl.key")]
		[DefaultValue(null)]
		public string ElasticsearchKey { get; set; }

		[YamlMember("elasticsearch.ssl.ca")]
		[DefaultValue(null)]
		public string ElasticsearchCA { get; set; }

		[YamlMember("status.allowAnonymous")]
		[DefaultValue(null)]
		public bool? AllowAnonymousAccess { get; set; }
	}
}
