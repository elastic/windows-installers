using System;
using System.Collections.Generic;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch
{
	public class Elasticsearch : Product
	{
		public override IEnumerable<string> AllArguments => ElasticsearchArgumentParser.AllArguments;

		public override IEnumerable<ModelArgument> MsiParams =>
			ElasticsearchInstallationModel.Create(new NoopWixStateProvider(), NoopSession.Elasticsearch).ToMsiParams();

		public override Dictionary<string, Guid> ProductCode => ProductGuids.ElasticsearchProductCodes;

		public override Guid UpgradeCode => ProductGuids.ElasticsearchUpgradeCode;

		public override EnvironmentVariable[] EnvironmentVariables =>
			new[]
			{
				new EnvironmentVariable(
					ElasticsearchEnvironmentStateProvider.EsHome,
					$"[{nameof(LocationsModel.InstallDir).ToUpperInvariant()}]")
				{
					Action = EnvVarAction.set,
					System = true
				},
				new EnvironmentVariable(
					ElasticsearchEnvironmentStateProvider.ConfDir,
					$"[{nameof(LocationsModel.ConfigDirectory).ToUpperInvariant()}]")
				{
					Action = EnvVarAction.set,
					System = true
				},
				// remove the old ES_CONFIG
				new EnvironmentVariable(
					ElasticsearchEnvironmentStateProvider.ConfDirOld, null)
				{
					Action = EnvVarAction.remove,
					System = true
				},
			};
	}
}
