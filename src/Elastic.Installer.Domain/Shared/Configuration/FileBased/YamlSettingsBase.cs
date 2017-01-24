using SharpYaml.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Shared.Configuration.FileBased
{
	public class YamlConfigurationBase<TSettings> where TSettings : class, IYamlSettings, new()
	{
		public YamlConfigurationBase(string path, IFileSystem fileSystem)
		{
			this.FileSystem = fileSystem ?? new FileSystem();
			this.Path = path;
			if (string.IsNullOrEmpty(path) || !this.FileSystem.File.Exists(this.Path)) return;

			try
			{
				var raw = this.FileSystem.File.ReadAllText(Path);

				//we flatten because neither yamldotnet nor sharpyaml can bind nested.props in all variants
				var dict = Flatten(this.Serializer.Deserialize<Dictionary<string, object>>(raw));
				var flattenedYaml = this.Serializer.Serialize(dict);

				this.YamlSettings = this.Serializer.Deserialize<TSettings>(flattenedYaml) ?? new TSettings();
			}
			catch
			{
				this.FoundButNotValid = true;
				this.YamlSettings = new TSettings();
			}
		}
		protected readonly Serializer Serializer = new Serializer(new SerializerSettings
		{
			SerializeDictionaryItemsAsMembers = true,
			EmitTags = false,
			EmitDefaultValues = false,
			EmitAlias = false
		});

		protected TSettings YamlSettings { get; set; } = new TSettings();
		protected string Path { get; set; }
		protected IFileSystem FileSystem { get; set; }
		public bool FoundButNotValid { get; protected set; }

		protected IDictionary<string, object> Flatten(IDictionary<string, object> parent)
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

		protected void Flatten(IDictionary<string, object> parent, IDictionary<object, object> child, string suffix)
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
				this.Serializer.Serialize(writer, this.YamlSettings);
				this.FileSystem.File.WriteAllText(this.Path, writer.ToString());
			}
		}
	}
}
