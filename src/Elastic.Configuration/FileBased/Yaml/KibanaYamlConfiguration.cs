using System.IO.Abstractions;
using static System.IO.Path;

namespace Elastic.Configuration.FileBased.Yaml
{
	public class KibanaYamlConfiguration : YamlConfigurationBase<KibanaYamlSettings>
	{
		public KibanaYamlSettings Settings => this.YamlSettings;

		private KibanaYamlConfiguration() : base (null, null) { }

		public KibanaYamlConfiguration(string path, IFileSystem fileSystem) : base(path, fileSystem) { }

		private static KibanaYamlConfiguration Default { get; } = new KibanaYamlConfiguration();

		public static KibanaYamlConfiguration FromFolder(string configDirectory) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new KibanaYamlConfiguration(Combine(configDirectory, "kibana.yml"), null);

		public static KibanaYamlConfiguration FromFolder(string configDirectory, IFileSystem fileSystem) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new KibanaYamlConfiguration(Combine(configDirectory, "kibana.yml"), fileSystem);
	}
}
