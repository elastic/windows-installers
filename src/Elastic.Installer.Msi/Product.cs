using Elastic.Installer.Domain.Model;
using Elastic.Installer.UI;
using System;
using System.Collections.Generic;
using WixSharp;

namespace Elastic.Installer.Msi
{
	public abstract class Product
	{
		public virtual Type EmbeddedUI => typeof(EmbeddedUI);

		public abstract Guid UpgradeCode { get; }

		public abstract Dictionary<string, Guid> ProductCode { get; }

		public abstract List<Dir> Files(string path);

		public abstract IEnumerable<string> AllArguments { get; }

		public abstract IEnumerable<ModelArgument> MsiParams { get; }
	}
}
