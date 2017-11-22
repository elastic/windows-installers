using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using WixSharp;
using WixSharp.CommonTasks;

namespace Elastic.Installer.Msi.Elasticsearch
{
	public class Elasticsearch : Product
	{
		private readonly bool _releaseMode;

		public Elasticsearch(bool releaseMode) => _releaseMode = releaseMode;

		/// <summary>
		/// Needed for generic custom actions.
		/// </summary>
		public Elasticsearch() { }

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

		public override void PatchProject(Project project)
		{
			var installAsServiceProperty = nameof(ServiceModel.InstallAsService).ToUpperInvariant();
			var startWhenWindowsStartsProperty = nameof(ServiceModel.StartWhenWindowsStarts).ToUpperInvariant();

			var elasticsearchExeFile = project
				.ResolveWildCards()
				.FindFile(f => f.Name.EndsWith("elasticsearch.exe"))
				.First();

			elasticsearchExeFile.Condition = new Condition("(NOT Installed) " +
				$"AND ({installAsServiceProperty}=0 OR {installAsServiceProperty}~=\"false\")");

			// directory path is *only* relevant to finding the directory specified by WixSharp,
			// in order to place the elasticsearch.exe Windows service components in the correct directory
			var serviceDirectory = project
				.FindDir($@"ProgramFiles64Folder\Elastic\Elasticsearch\{project.Version}\bin");

			var autoStartServiceExeFile = new File(new Id(elasticsearchExeFile.Id + ".auto"), elasticsearchExeFile.Name)
			{
				Condition = new Condition($"(NOT Installed) " +
					$"AND ({installAsServiceProperty}~=\"true\" OR {installAsServiceProperty}=1) " +
				    $"AND ({startWhenWindowsStartsProperty}~=\"true\" OR {startWhenWindowsStartsProperty}=1)"),
				ServiceInstaller = new ServiceInstaller(ServiceModel.ElasticsearchServiceName)
				{
					Id = "AutoStartElasticsearch",
					Description = "You know, for Search.",
					Interactive = false,
					StartType = SvcStartType.auto,
					Account = $"[{ServiceModel.ServiceAccount}]",
					Password = $"[{ServiceModel.ServicePassword}]",
					Type = SvcType.ownProcess,
					Vital = true,
					StartOn = SvcEvent.Install_Wait,
					StopOn = SvcEvent.InstallUninstall_Wait,
					RemoveOn = SvcEvent.Uninstall_Wait,
				}
			};

			serviceDirectory.AddFile(autoStartServiceExeFile);

			var manualStartServiceExeFile = new File(new Id(elasticsearchExeFile.Id + ".manual"), elasticsearchExeFile.Name)
			{
				Condition = new Condition($"(NOT Installed) " +
					$"AND ({installAsServiceProperty}~=\"true\" OR {installAsServiceProperty}=1) " +
				    $"AND ({startWhenWindowsStartsProperty}~=\"false\" OR {startWhenWindowsStartsProperty}=0)"),
				ServiceInstaller = new ServiceInstaller(ServiceModel.ElasticsearchServiceName)
				{
					Id = "ManualStartElasticsearch",
					Description = "You know, for Search.",
					Interactive = false,
					StartType = SvcStartType.demand,
					Account = $"[{ServiceModel.ServiceAccount}]",
					Password = $"[{ServiceModel.ServicePassword}]",
					Type = SvcType.ownProcess,
					Vital = true,
					StartOn = SvcEvent.Install_Wait,
					StopOn = SvcEvent.InstallUninstall_Wait,
					RemoveOn = SvcEvent.Uninstall_Wait,
				}
			};

			serviceDirectory.AddFile(manualStartServiceExeFile);

			var servicePasswordProperty = new Property(ServiceModel.ServicePassword, string.Empty)
			{
				Attributes = new Dictionary<string, string> { { "Hidden", "yes" } }
			};

			project.Properties = project.Properties.Concat(new[] { servicePasswordProperty }).ToArray();
		}

		public override void PatchWixSource(XDocument document)
		{
			var documentRoot = document.Root;
			var ns = documentRoot.Name.Namespace;

			var directories = documentRoot.Descendants(ns + "Directory")
				.Where(c =>
				{
					var childComponents = c.Elements(ns + "Component");
					return childComponents.Any() && childComponents.Any(cc => cc.Descendants(ns + "File").Any());
				});

			var feature = documentRoot.Descendants(ns + "Feature").Single(f => f.Attribute("Id").Value == "Complete");
			var re = new Regex(@"\.\d+$");
			foreach (var directory in directories)
			{
				var directoryId = directory.Attribute("Id").Value;
				var componentId = "Component." + directoryId;
				var directoryName = directory.Attribute("Name").Value;

				// WixSharp appends a .{DIGIT} to duplicate file names in different directories in installer e.g. plugin_descriptor.properties
				// for Elasticsearch plugins. Problem is duplicated file names may not represent the same file path across versions so
				// fix this by including the directory name in the file name.
				var duplicateNamedComponents = directory.Elements(ns + "Component")
					.Where(c => re.IsMatch(c.Attribute("Id").Value));

				foreach (var component in duplicateNamedComponents)
				{
					PatchDuplicateComponentId(ns, component, directoryName);
				}

				directory.AddFirst(new XElement(ns + "Component",
					new XAttribute("Id", componentId),
					new XAttribute("Guid", WixGuid.NewGuid(componentId)),
					new XAttribute("Win64", "yes"),
					new XElement(ns + "RemoveFile",
						new XAttribute("Id", directoryId + ".all"),
						new XAttribute("Name", "*"), // remove all files in dir
						new XAttribute("On", "both")
					),
					new XElement(ns + "RemoveFolder",
						new XAttribute("Id", directoryId + ".dir"), // remove (now empty) dir
						new XAttribute("On", "both")
					)
				));

				// Add a Directory entry to remove all files in bin/x-pack, if present
				if (directoryId == "INSTALLDIR.bin")
				{
					var installdirBinXpack = "INSTALLDIR.bin.xpack";
					var componentInstalldirBinXpack = $"Component.{installdirBinXpack}";
					directory.AddFirst(new XElement(ns + "Directory",
						new XAttribute("Id", installdirBinXpack),
						new XAttribute("Name", "x-pack"),
						new XElement(ns + "Component",
							new XAttribute("Id", componentInstalldirBinXpack),
							new XAttribute("Guid", WixGuid.NewGuid(componentInstalldirBinXpack)),
							new XAttribute("Win64", "yes"),
							new XElement(ns + "RemoveFile",
								new XAttribute("Id", installdirBinXpack),
								new XAttribute("Name", "*"), // remove all files in x-pack dir
								new XAttribute("On", "both")
							),
							new XElement(ns + "RemoveFolder",
								new XAttribute("Id", installdirBinXpack + ".dir"), // remove (now empty) x-pack dir
								new XAttribute("On", "both")
							)
						)
					));

					feature.Add(new XElement(ns + "ComponentRef", new XAttribute("Id", componentInstalldirBinXpack)));
				}

				feature.Add(new XElement(ns + "ComponentRef", new XAttribute("Id", componentId)));
			}

			// include WixFailWhenDeferred Custom Action when not building a release
			// see http://wixtoolset.org/documentation/manual/v3/customactions/wixfailwhendeferred.html
			if (!_releaseMode)
			{
				var product = documentRoot.Descendants(ns + "Product").First();
				product.Add(new XElement(ns + "CustomActionRef",
						new XAttribute("Id", "WixFailWhenDeferred")
					)
				);
			}

			// Add condition to InstallServices
			var installExecuteSequence = documentRoot.Descendants(ns + "InstallExecuteSequence").Single();
			var installAsServiceProperty = nameof(ServiceModel.InstallAsService).ToUpperInvariant();
			var startAfterInstallProperty = nameof(ServiceModel.StartAfterInstall).ToUpperInvariant();

			installExecuteSequence.Add(new XElement(ns + "InstallServices",
				// Sequence number pinned from inspecting MSI with Orca
				new XAttribute("Sequence", "5800"),
				new XCData($"VersionNT AND (NOT Installed) " +
				           $"AND ({installAsServiceProperty}~=\"true\" OR {installAsServiceProperty}=1)")
			));

			// Add condition to StartServices	
			installExecuteSequence.Add(new XElement(ns + "StartServices",
				// Sequence number pinned from inspecting MSI with Orca
				new XAttribute("Sequence", "5900"),
				new XCData($"VersionNT AND (NOT Installed) " +
				           $"AND ({installAsServiceProperty}~=\"true\" OR {installAsServiceProperty}=1) " +
				           $"AND ({startAfterInstallProperty}~=\"true\" OR {startAfterInstallProperty}=1)")
			));

			// Update in-built progress templates 
			// See http://web.mit.edu/ops/services/afs/openafs-1.4.1/src/src/WINNT/install/wix/lang/en_US/ActionText.wxi
			var ui = documentRoot.Descendants(ns + "UI").Single();
			ui.Add(new XElement(ns + "ProgressText",
				new XAttribute("Action", "InstallFiles"),
				new XAttribute("Template", "Copying new files: [9][1]"),
				new XText("Copying new files")
			), new XElement(ns + "ProgressText",
				new XAttribute("Action", "StopServices"),
				new XAttribute("Template", "Stopping Elasticsearch service"),
				new XText("Stopping Elasticsearch service")
			), new XElement(ns + "ProgressText",
				new XAttribute("Action", "StartServices"),
				new XAttribute("Template", "Starting Elasticsearch service"),
				new XText("Starting Elasticsearch service")
			), new XElement(ns + "ProgressText",
				new XAttribute("Action", "InstallServices"),
				new XAttribute("Template", "Installing Elasticsearch service"),
				new XText("Installing Elasticsearch service")
			), new XElement(ns + "ProgressText",
				new XAttribute("Action", "DeleteServices"),
				new XAttribute("Template", "Removing Elasticsearch service"),
				new XText("Removing Elasticsearch service")
			));
		}

		private static void PatchComponentRef(XNamespace ns, XElement component, string oldId, string newId)
		{
			var root = component.Document.Root;
			var componentRef = root.Descendants(ns + "ComponentRef").First(d => d.HasAttribute("Id", oldId));
			componentRef.Attribute("Id").Remove();
			componentRef.Add(new XAttribute("Id", newId));
		}

		private static void PatchDuplicateComponentFileId(XNamespace ns, XElement component, string newId)
		{
			var componentFile = component.Element(ns + "File");
			var fileAttribute = componentFile?.Attribute("Id");
			if (fileAttribute == null) return;

			fileAttribute.Remove();
			componentFile.Add(new XAttribute("Id", newId));
		}

		private static void PatchDuplicateComponentId(XNamespace ns, XElement component, string directoryName)
		{
			var idAttribute = component.Attribute("Id");
			if (idAttribute == null) return;

			var id = idAttribute.Value;
			idAttribute.Remove();

			// include directory name in Component Id
			var newId = Regex.Replace(id, @"Component\.(.+)\.\d+", $"{directoryName}.$1").Replace("-", "_");
			var newComponentId = $"Component.{newId}";
			component.Add(new XAttribute("Id", newComponentId));

			// generate a new guid based on the new Component Id
			component.Attribute("Guid").Value = WixGuid.NewGuid(newComponentId).ToString().ToLowerInvariant();

			PatchDuplicateComponentFileId(ns, component, newId);
			PatchComponentRef(ns, component, id, newComponentId);
		}
	}
}
