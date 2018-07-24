using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch.Closing;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Model.Elasticsearch.Notice;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;

namespace Elastic.Installer.Domain.Model.Elasticsearch
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
			typeof(XPackModel),
			typeof(ClosingModel)
		};

		public static IEnumerable<string> AllArguments { get; } = GetAllArguments(ExpectedTypes);

		public static IDictionary<Type, IEnumerable<string>> ArgumentsByModel { get; } = GetArgumentsByModel(ExpectedTypes);

		public ElasticsearchArgumentParser(IList<IValidatableReactiveObject> models, string[] args) : base(models, args)
		{
			if (models.Count != ExpectedTypes.Length)
				throw new ArgumentException($"{nameof(models)} should provide an instance of all the expected types");
		}
	}
}