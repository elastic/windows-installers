using System;
using System.Collections.Generic;
using Elastic.Installer.Domain;
using Elastic.Installer.UI;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch
{
	public class Elasticsearch : Product
	{
		public override Dictionary<string, Guid> ProductCode => ProductGuids.ElasticsearchProductCodes;

		public override Guid UpgradeCode => ProductGuids.ElasticsearchUpgradeCode;

		public override List<Dir> Files(string path) => new List<Dir>
		{
			new Dir("bin", new Files(path + @"\bin\*.*")),
			new Dir("lib", new Files(path + @"\lib\*.*")),
			new Dir("config", new Files(path + @"\config\*.*")),
			new Dir("modules", new Files(path + @"\modules\*.*"))
		};
	}
}
