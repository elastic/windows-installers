using System.IO.Abstractions;
using static System.IO.Path;

namespace Elastic.Configuration.FileBased.Yaml
{
	public class ElasticsearchYamlConfiguration : YamlConfigurationBase<ElasticsearchYamlSettings>
	{
		public ElasticsearchYamlSettings Settings => this.YamlSettings;

		public ElasticsearchYamlConfiguration(string path, IFileSystem fileSystem) : base(path, fileSystem) { }

		private static ElasticsearchYamlConfiguration Default { get; } = new ElasticsearchYamlConfiguration(null, null);

		public static ElasticsearchYamlConfiguration FromFolder(string configDirectory) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new ElasticsearchYamlConfiguration(Combine(configDirectory, "elasticsearch.yml"), null);

		public static ElasticsearchYamlConfiguration FromFolder(string configDirectory, IFileSystem fileSystem) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new ElasticsearchYamlConfiguration(Combine(configDirectory, "elasticsearch.yml"), fileSystem);
	}
}
