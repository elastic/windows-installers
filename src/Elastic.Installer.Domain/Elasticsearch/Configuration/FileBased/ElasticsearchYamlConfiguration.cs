using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using SharpYaml.Serialization;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased
{
	public class ElasticsearchYamlConfiguration
	{

		private readonly Serializer _serializer = new Serializer(new SerializerSettings
		{
			SerializeDictionaryItemsAsMembers = true,
			EmitTags = false,
			EmitDefaultValues = false,
		});

		private readonly string _path;
		private readonly IFileSystem _fileSystem;

		public ElasticsearchYamlSettings Settings { get; } = new ElasticsearchYamlSettings();
		public bool FoundButNotValid { get; }

		public ElasticsearchYamlConfiguration(string path) : this(path, null) { }
		public ElasticsearchYamlConfiguration(string path, IFileSystem fileSystem)
		{
			this._fileSystem = fileSystem ?? new FileSystem();
			this._path = path;
			if (string.IsNullOrEmpty(path) || !this._fileSystem.File.Exists(this._path)) return;

			try
			{

				var raw = this._fileSystem.File.ReadAllText(_path);

				//we flatten because neither yamldotnet nor sharpyaml can bind nested.props in all variants
				var dict = Flatten(this._serializer.Deserialize<Dictionary<string, object>>(raw));
				var flattenedYaml = this._serializer.Serialize(dict);

				this.Settings = this._serializer.Deserialize<ElasticsearchYamlSettings>(flattenedYaml) ?? new ElasticsearchYamlSettings();
			}
			catch
			{
				this.FoundButNotValid = true;
				this.Settings = new ElasticsearchYamlSettings();
			}
		}

		private static IDictionary<string, object> Flatten(IDictionary<string, object> parent)
		{
			var newDictionary = new Dictionary<string, object>();
			if (parent == null) return newDictionary;
			foreach (var kv in parent)
			{
				var value = kv.Value as IDictionary<object, object>;
				if (value != null)
				{
					Flatten(newDictionary, value, (string)kv.Key);
					continue;
				}
				newDictionary.Add(kv.Key, kv.Value);
			}
			return newDictionary;
		}

		private static void Flatten(IDictionary<string, object> parent, IDictionary<object, object> child, string suffix)
		{
			if (child == null) return;
			foreach (var kv in child)
			{
				var value = kv.Value as IDictionary<object, object>;
				if (value != null)
				{
					Flatten(parent, value, suffix + "." + (string)kv.Key);
					continue;
				}
				parent.Add(suffix + "." + kv.Key, kv.Value);
			}
		}

		public void Save()
		{
			using (var writer = new StringWriter())
			{
				this._serializer.Serialize(writer, Settings);
				this._fileSystem.File.WriteAllText(_path, writer.ToString());
			}
		}

		public static ElasticsearchYamlConfiguration Default { get; } = new ElasticsearchYamlConfiguration(null);

		public static ElasticsearchYamlConfiguration FromFolder(string configDirectory) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new ElasticsearchYamlConfiguration(Path.Combine(configDirectory, "elasticsearch.yml"), null);

		public static ElasticsearchYamlConfiguration FromFolder(string configDirectory, IFileSystem fileSystem) =>
			string.IsNullOrEmpty(configDirectory)
			? Default
			: new ElasticsearchYamlConfiguration(Path.Combine(configDirectory, "elasticsearch.yml"), fileSystem);

	}
}
