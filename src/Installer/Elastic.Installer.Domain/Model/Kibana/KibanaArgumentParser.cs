using System;
using System.Collections.Generic;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch.Closing;
using Elastic.Installer.Domain.Model.Kibana.Configuration;
using Elastic.Installer.Domain.Model.Kibana.Connecting;
using Elastic.Installer.Domain.Model.Kibana.Locations;
using Elastic.Installer.Domain.Model.Kibana.Notice;
using Elastic.Installer.Domain.Model.Kibana.Plugins;

namespace Elastic.Installer.Domain.Model.Kibana
{
	public class KibanaArgumentParser : ModelArgumentParser
	{
		public static Type[] ExpectedTypes = 
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
			if (models.Count != ExpectedTypes.Length)
				throw new ArgumentException($"{nameof(models)} should provide an instance of all the expected types");
		}	
	}
}