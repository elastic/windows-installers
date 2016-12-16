using System;
using System.Collections.Generic;
using Elastic.Installer.Domain;
using WixSharp;

namespace Elastic.Installer.Msi.Kibana
{
	public class KibanaProduct : Product
	{
		public override Dictionary<string, Guid> ProductCode => ProductGuids.KibanaProductCodes;

		public override Guid UpgradeCode => ProductGuids.KibanaUpgradeCode;

		public override List<Dir> Files(string path) => new List<Dir>
		{
			new Dir("bin", new Files(path + @"\bin\*.*")),
			new Dir("config", new Files(path + @"\config\*.*")),
			new Dir("data", new Files(path + @"\data\*.*")),
			new Dir("node", new Files(path + @"\node\*.*")),
			new Dir("node_modules", new Files(path + @"\node_modules\*.*")),
			new Dir("optimize", new Files(path + @"\optimize\*.*")),
			new Dir("plugins", new Files(path + @"\plugins\*.*")),
			new Dir("src", new Files(path + @"\src\*.*")),
			new Dir("webpackShims", new Files(path + @"\webpackShims\*.*"))
		};
	}
}
