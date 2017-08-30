using System;
using System.Collections.Generic;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Model.Elasticsearch;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch
{
	public class Elasticsearch : Product
	{
		public override IEnumerable<string> AllArguments => ElasticsearchArgumentParser.AllArguments;

		public override IEnumerable<ModelArgument> MsiParams =>
			ElasticsearchInstallationModel.Create(new NoopWixStateProvider(), new NoopSession()).ToMsiParams();

		public override Dictionary<string, Guid> ProductCode => ProductGuids.ElasticsearchProductCodes;

		public override Guid UpgradeCode => ProductGuids.ElasticsearchUpgradeCode;

		public override List<Dir> Files(string path, string companionFile)
		{
			var dirs = base.Files(path, companionFile);
			dirs.Add(new Dir("plugins"));
			return dirs;
		}
	}
}
