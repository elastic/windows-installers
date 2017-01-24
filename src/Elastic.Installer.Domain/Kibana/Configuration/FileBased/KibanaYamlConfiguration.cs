using Elastic.Installer.Domain.Shared.Configuration.FileBased;
using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Configuration.FileBased
{
	public class KibanaYamlConfiguration : YamlConfigurationBase<KibanaYamlSettings>
	{
		public KibanaYamlSettings Settings { get { return this.YamlSettings as KibanaYamlSettings; } }

		internal KibanaYamlConfiguration() : base (null, null) { }

		public KibanaYamlConfiguration(KibanaYamlSettings settings)
			: base(null, null)
		{
			this.YamlSettings = settings;
		}

		public KibanaYamlConfiguration(string path) : base(path, null) { }

		public KibanaYamlConfiguration(string path, IFileSystem fileSystem) : base(path, fileSystem) { }

		public static KibanaYamlConfiguration Default { get; } = new KibanaYamlConfiguration();

		public static KibanaYamlConfiguration FromFolder(string configDirectory) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new KibanaYamlConfiguration(System.IO.Path.Combine(configDirectory, "kibana.yml"), null);

		public static KibanaYamlConfiguration FromFolder(string configDirectory, IFileSystem fileSystem) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new KibanaYamlConfiguration(System.IO.Path.Combine(configDirectory, "kibana.yml"), fileSystem);
	}
}
