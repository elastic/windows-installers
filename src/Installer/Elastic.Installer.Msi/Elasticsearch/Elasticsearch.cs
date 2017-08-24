using System;
using System.Collections.Generic;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Model.Elasticsearch;

namespace Elastic.Installer.Msi.Elasticsearch
{
	public class Elasticsearch : Product
	{
		public override IEnumerable<string> AllArguments => ElasticsearchArgumentParser.AllArguments;

		public override IEnumerable<ModelArgument> MsiParams =>
			ElasticsearchInstallationModel.Create(new NoopWixStateProvider(Domain.Product.Elasticsearch, null), new NoopSession()).ToMsiParams();

		public override Dictionary<string, Guid> ProductCode => ProductGuids.ElasticsearchProductCodes;

		public override Guid UpgradeCode => ProductGuids.ElasticsearchUpgradeCode;
	}
}
