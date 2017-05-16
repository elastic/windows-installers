using System;
using System.Collections.Generic;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Session;

namespace Elastic.Installer.Msi.Elasticsearch
{
	public class Elasticsearch : Product
	{
		public override IEnumerable<string> AllArguments => ElasticsearchArgumentParser.AllArguments;

		public override IEnumerable<ModelArgument> MsiParams =>
			ElasticsearchInstallationModel.Create(new NoopWixStateProvider(), new NoopSession()).ToMsiParams();

		public override Dictionary<string, Guid> ProductCode => ProductGuids.ElasticsearchProductCodes;

		public override Guid UpgradeCode => ProductGuids.ElasticsearchUpgradeCode;
	}
}
