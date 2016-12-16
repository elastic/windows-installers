using System;
using System.Collections.Generic;
using WixSharp;

namespace Elastic.Installer.Msi
{
	public abstract class Product
	{
		public virtual Type EmbeddedUI { get; }

		public abstract Guid UpgradeCode { get; }

		public abstract Dictionary<string, Guid> ProductCode { get; }

		public abstract List<Dir> Files(string path);
	}
}
