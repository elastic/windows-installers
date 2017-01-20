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
using Elastic.Installer.Domain.Elasticsearch.Model.Plugins;

namespace Elastic.Installer.Domain.Elasticsearch.Model
{
	public class ElasticsearchArgumentParser : ModelArgumentParser
	{
		public static Type[] ExpectedTypes = new[]
		{
			typeof(ElasticsearchInstallationModel),
			typeof(NoticeModel),
			typeof(LocationsModel),
			typeof(ConfigurationModel),
			typeof(ServiceModel),
			typeof(PluginsModel),
			typeof(ClosingModel)
		};

		public static IEnumerable<string> AllArguments { get; } = GetAllArguments(ExpectedTypes);

		public static IDictionary<Type, IEnumerable<string>> ArgumentsByModel { get; } = GetArgumentsByModel(ExpectedTypes);

		public ElasticsearchArgumentParser(IList<IValidatableReactiveObject> models, string[] args) : base(models, args)
		{
			if (models.Count != ExpectedTypes.Count())
				throw new ArgumentException($"{nameof(models)} should provide an instance of all the expected types");
		}
	}
}