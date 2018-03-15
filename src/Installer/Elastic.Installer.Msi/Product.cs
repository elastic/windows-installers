using Elastic.Installer.Domain.Model;
using Elastic.Installer.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WixSharp;

namespace Elastic.Installer.Msi
{
	public abstract class Product
	{
		public virtual Type EmbeddedUI => typeof(EmbeddedUI);

		public abstract Guid UpgradeCode { get; }

		/// <summary>
		/// The registry key used to persist values in the registry needed for uninstall
		/// </summary>
		public string RegistryKey => @"SOFTWARE\[Manufacturer]\[ElasticProduct]\[CurrentVersion]";

		/// <summary>
		/// The version to product codes
		/// </summary>
		public abstract Dictionary<string, Guid> ProductCode { get; }

		public abstract IEnumerable<string> AllArguments { get; }

		public abstract IEnumerable<ModelArgument> MsiParams { get; }

		public virtual EnvironmentVariable[] EnvironmentVariables { get; }

		public List<Dir> Files(string path) =>
			Directory.GetDirectories(path)
				.Where(directory => Directory.EnumerateFileSystemEntries(directory).Any())
				.Select(Path.GetFileName)
				.Select(directoryName => new Dir(directoryName, new Files(path + $@"\{directoryName}\*.*")))
				.ToList();

		public virtual void PatchWixSource(XDocument document) { }

		public virtual void PatchProject(Project project) { }
	}
}
