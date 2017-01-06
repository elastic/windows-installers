using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Installer.Domain.Elasticsearch.Model.Closing;
using Elastic.Installer.Domain.Elasticsearch.Model.Config;
using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using Elastic.Installer.Domain.Elasticsearch.Model.Notice;
using Elastic.Installer.Domain.Shared.Model.Plugins;
using Elastic.Installer.Domain.Shared.Model.Service;
using Elastic.Installer.Domain.Model;

namespace Elastic.Installer.Domain.Elasticsearch.Model
{
	public class InstallationModelArgumentParser : ModelArgumentParser
	{
		public static Type[] ExpectedTypes = new[]
		{
			typeof(InstallationModel),
			typeof(NoticeModel),
			typeof(LocationsModel),
			typeof(ConfigurationModel),
			typeof(ServiceModel),
			typeof(PluginsModel),
			typeof(ClosingModel)
		};

		public static IEnumerable<string> AllArguments { get; } = _allArguments();

		public static IDictionary<Type, IEnumerable<string>> ArgumentsByModel { get; } = _argumentsByModel();

		public InstallationModelArgumentParser(IList<IValidatableReactiveObject> models, string[] args) : base(models, args)
		{
			if (models.Count != ExpectedTypes.Count())
				throw new ArgumentException($"{nameof(models)} should provide an instance of all the expected types");
		}

		private static IDictionary<Type, IEnumerable<string>> _argumentsByModel()
		{
			var argumentsByType = new Dictionary<Type, IEnumerable<string>>();
			foreach (var type in ExpectedTypes)
			{
				var seenNames = new HashSet<string>();
				var viewModelProperties = GetProperties(type);
				foreach (var p in viewModelProperties)
				{
					if (seenNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
						throw new ArgumentException($"{p.Name} can not be reused as argument option on {type.Name}");
					seenNames.Add(p.Name.ToUpperInvariant());
				}
				argumentsByType.Add(type, seenNames);
			}
			return argumentsByType;
		}

		private static IEnumerable<string> _allArguments()
		{
			var seenNames = new HashSet<string>();
			foreach (var type in ExpectedTypes)
			{
				var viewModelProperties = GetProperties(type);
				foreach (var p in viewModelProperties)
				{
					if (seenNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
						throw new ArgumentException($"{p.Name} can not be reused as argument option on {type.Name}");
					seenNames.Add(p.Name.ToUpperInvariant());
				}
			}

			return seenNames;
		}
	}
}