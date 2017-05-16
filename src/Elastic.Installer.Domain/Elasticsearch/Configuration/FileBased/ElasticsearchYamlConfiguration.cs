using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using SharpYaml.Serialization;
using Elastic.Installer.Domain.Shared.Configuration.FileBased;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased
{
	public class ElasticsearchYamlConfiguration : YamlConfigurationBase<ElasticsearchYamlSettings>
	{
		public ElasticsearchYamlSettings Settings { get { return this.YamlSettings as ElasticsearchYamlSettings; } }

		public ElasticsearchYamlConfiguration(string path) : base(path, null) { }
		public ElasticsearchYamlConfiguration(string path, IFileSystem fileSystem) : base(path, fileSystem) { }

		public static ElasticsearchYamlConfiguration Default { get; } = new ElasticsearchYamlConfiguration(null);

		public static ElasticsearchYamlConfiguration FromFolder(string configDirectory) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new ElasticsearchYamlConfiguration(System.IO.Path.Combine(configDirectory, "elasticsearch.yml"), null);

		public static ElasticsearchYamlConfiguration FromFolder(string configDirectory, IFileSystem fileSystem) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new ElasticsearchYamlConfiguration(System.IO.Path.Combine(configDirectory, "elasticsearch.yml"), fileSystem);
	}
}
