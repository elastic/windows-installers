using System.Collections.Generic;
using System.ComponentModel;
using SharpYaml.Serialization;

namespace Elastic.Configuration.FileBased.Yaml
{
	public class KibanaYamlSettings : Dictionary<string, object>, IYamlSettings
	{
		[YamlMember("server.host")]
		[DefaultValue("")]
		public string ServerHost { get; set; }

		[YamlMember("server.port")]
		[DefaultValue("")]
		public int? ServerPort { get; set; }

		[YamlMember("server.basePath")]
		[DefaultValue("")]
		public string ServerBasePath { get; set; }

		[YamlMember("server.name")]
		[DefaultValue("")]
		public string ServerName { get; set; }

		[YamlMember("server.defaultRoute")]
		[DefaultValue("")]
		public string ServerDefaultRoute { get; set; }

		[YamlMember("logging.dest")]
		[DefaultValue("")]
		public string LoggingDestination { get; set; }

		[YamlMember("elasticsearch.url")]
		[DefaultValue("")]
		public string ElasticsearchUrl { get; set; }

		[YamlMember("kibana.index")]
		[DefaultValue("")]
		public string KibanaIndex { get; set; }

		[YamlMember("server.ssl.cert")]
		[DefaultValue("")]
		public string ServerCert { get; set; }

		[YamlMember("server.ssl.key")]
		[DefaultValue("")]
		public string ServerKey { get; set; }

		[YamlMember("elasticsearch.ssl.cert")]
		[DefaultValue("")]
		public string ElasticsearchCert { get; set; }

		[YamlMember("elasticsearch.ssl.key")]
		[DefaultValue("")]
		public string ElasticsearchKey { get; set; }

		[YamlMember("elasticsearch.ssl.ca")]
		[DefaultValue("")]
		public string ElasticsearchCA { get; set; }

		[YamlMember("status.allowAnonymous")]
		[DefaultValue(null)]
		public bool? AllowAnonymousAccess { get; set; }
	}
}
