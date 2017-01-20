using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Installer.Domain.Shared.Model.Plugins;
using Elastic.Installer.Domain.Shared.Model.Service;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Closing;
using Elastic.Installer.Domain.Kibana.Model.Locations;
using Elastic.Installer.Domain.Kibana.Model.Configuration;
using Elastic.Installer.Domain.Kibana.Model.Connecting;
using Elastic.Installer.Domain.Kibana.Model.Plugins;
using Elastic.Installer.Domain.Kibana.Model.Notice;

namespace Elastic.Installer.Domain.Kibana.Model
{
	public class KibanaArgumentParser : ModelArgumentParser
	{
		public static Type[] ExpectedTypes = new[]
		{
			typeof(KibanaInstallationModel),
			typeof(NoticeModel),
			typeof(LocationsModel),
			typeof(ServiceModel),
			typeof(ConfigurationModel),
			typeof(ConnectingModel),
			typeof(PluginsModel),
			typeof(ClosingModel)
		};

		public static IEnumerable<string> AllArguments { get; } = GetAllArguments(ExpectedTypes);

		public static IDictionary<Type, IEnumerable<string>> ArgumentsByModel { get; } = GetArgumentsByModel(ExpectedTypes);

		public KibanaArgumentParser(IList<IValidatableReactiveObject> models, string[] args) : base(models, args)
		{
			if (models.Count != ExpectedTypes.Count())
				throw new ArgumentException($"{nameof(models)} should provide an instance of all the expected types");
		}	
	}
}