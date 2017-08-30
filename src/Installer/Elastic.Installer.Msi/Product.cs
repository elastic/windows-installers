using Elastic.Installer.Domain.Model;
using Elastic.Installer.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WixSharp;

namespace Elastic.Installer.Msi
{
	public abstract class Product
	{
		public virtual Type EmbeddedUI => typeof(EmbeddedUI);

		public abstract Guid UpgradeCode { get; }

		public abstract Dictionary<string, Guid> ProductCode { get; }

		public abstract IEnumerable<string> AllArguments { get; }

		public abstract IEnumerable<ModelArgument> MsiParams { get; }

		public virtual List<Dir> Files(string path, string companionFile) =>
			Directory.GetDirectories(path)
				.Where(directory => Directory.EnumerateFileSystemEntries(directory).Any())
				.Select(Path.GetFileName)
				.Select(directoryName => new Dir(directoryName, new Files(path + $@"\{directoryName}\*.*")))
				.ToList();
	}
}
