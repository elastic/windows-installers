using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.Win32;
using WixSharp;

namespace Elastic.Installer.Msi.Elasticsearch
{
	public class Elasticsearch : Product
	{
		private static readonly string InstallAsServiceProperty = nameof(ServiceModel.InstallAsService).ToUpperInvariant();
		private static readonly string StartAfterInstallProperty = nameof(ServiceModel.StartAfterInstall).ToUpperInvariant();
		private static readonly string InstallDirProperty = nameof(LocationsModel.InstallDir).ToUpperInvariant();
		private static readonly string ConfigDirectoryProperty = nameof(LocationsModel.ConfigDirectory).ToUpperInvariant();

		private IEnumerable<ModelArgument> _msiParams;
		
		public override IEnumerable<string> AllArguments => ElasticsearchArgumentParser.AllArguments;

		public override IEnumerable<ModelArgument> MsiParams => _msiParams ?? (_msiParams = 
			ElasticsearchInstallationModel.Create(new NoopWixStateProvider(), NoopSession.Elasticsearch).ToMsiParams());

		public override Dictionary<string, Guid> ProductCode => ProductGuids.ElasticsearchProductCodes;

		public override Guid UpgradeCode => ProductGuids.ElasticsearchUpgradeCode;

		public override EnvironmentVariable[] EnvironmentVariables =>
			new[]
			{
				new EnvironmentVariable(
					new Id($"EnvVar.{ElasticsearchEnvironmentStateProvider.EsHome}"),
					ElasticsearchEnvironmentStateProvider.EsHome,
					$"[{InstallDirProperty}]")
				{
					Action = EnvVarAction.set,
					System = true
				},
				new EnvironmentVariable(
					new Id($"EnvVar.{ElasticsearchEnvironmentStateProvider.ConfDir}"),
					ElasticsearchEnvironmentStateProvider.ConfDir,
					$"[{ConfigDirectoryProperty}]")
				{
					Action = EnvVarAction.set,
					System = true
				},
				// remove the old ES_CONFIG
				new EnvironmentVariable(
					new Id($"EnvVar.{ElasticsearchEnvironmentStateProvider.ConfDirOld}"),
					ElasticsearchEnvironmentStateProvider.ConfDirOld, null)
				{
					Action = EnvVarAction.remove,
					System = true
				},
			};

		public override void PatchProject(Project project)
		{
			var elasticsearchExeFile = project
				.ResolveWildCards()
				.FindFile(f => f.Name.EndsWith("elasticsearch.exe"))
				.First();

			// *actual* service installation and start/stop/remove are controlled through
			// conditions on those actions in the InstallExecuteSequence table
			elasticsearchExeFile.ServiceInstaller = new ServiceInstaller(ServiceModel.ElasticsearchServiceName)
			{
				Id = "ElasticsearchService",
				Description = "You know, for Search.",
				Interactive = false,
				StartType = SvcStartType.auto, // starttype is not parameterizable, changed in custom action
				Account = $"[{ServiceModel.ServiceAccount}]",
				Password = $"[{ServiceModel.ServicePassword}]",
				Type = SvcType.ownProcess,
				Vital = true,
				StartOn = SvcEvent.Install_Wait,
				StopOn = SvcEvent.InstallUninstall_Wait,
				RemoveOn = SvcEvent.Uninstall_Wait,
			};

			// don't emit the ServicePassword property in logs
			var servicePasswordProperty = new Property(ServiceModel.ServicePassword, string.Empty)
			{
				AttributesDefinition = "Hidden=yes"
			};

			project.Properties = project.Properties.Concat(new[] { servicePasswordProperty }).ToArray();

			// persist properties in registry for use on upgrade/uninstall
			var regValues = MsiParams
				.Where(msiParam => msiParam.Attribute.PersistInRegistry)
				.Select(msiParam => 
					new RegValue(
						new Id($"Registry.{msiParam.Attribute.Name}"),
						RegistryHive.LocalMachine, 
						RegistryKey, 
						msiParam.Attribute.Name, 
						$"[{msiParam.Attribute.Name}]")
					{
						AttributesDefinition = "Type=string"
					});

			project.RegValues = project.RegValues.Concat(regValues).ToArray();
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

			var product = documentRoot.Descendants(ns + "Product").Single();
			var feature = documentRoot.Descendants(ns + "Feature").First(f => f.Attribute("Id").Value == "Complete");

			var duplicateOrAbbreviatedRegex = new Regex(@"\.\d+$|\._\.\.\.(.+)$");
			foreach (var directory in directories)
			{
				var directoryId = directory.Attribute("Id").Value;
				var componentId = "Component." + directoryId;
				var directoryName = directory.Attribute("Name").Value;

				// WixSharp appends a .{DIGIT} to duplicate file names in different directories in installer e.g. plugin_descriptor.properties
				// for Elasticsearch plugins. Problem is duplicated file names may not represent the same file path across versions so
				// fix this by including the directory name in the file name.
				var renameComponents = directory.Elements(ns + "Component")
					.Where(c => duplicateOrAbbreviatedRegex.IsMatch(c.Attribute("Id").Value));

				foreach (var component in renameComponents)
					PatchDuplicateComponentId(ns, component, directoryId);

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
				// Add an empty plugins directory
				else if (directoryId == InstallDirProperty)
				{
					var installdirPlugins = "INSTALLDIR.plugins";
					var componentInstalldirPlugins = $"Component.{installdirPlugins}";

					// Add elements to remove the plugins folder
					directory.AddFirst(new XElement(ns + "Directory",
						new XAttribute("Id", installdirPlugins),
						new XAttribute("Name", "plugins"),
						new XElement(ns + "Component",
							new XAttribute("Id", componentInstalldirPlugins),
							new XAttribute("Guid", WixGuid.NewGuid(componentInstalldirPlugins)),
							new XAttribute("Win64", "yes"),
							new XElement(ns + "RemoveFile",
								new XAttribute("Id", installdirPlugins),
								new XAttribute("Name", "*"), // remove all top level files in plugins dir
								new XAttribute("On", "both")
							),
							new XElement(ns + "RemoveFolder",
								new XAttribute("Id", installdirPlugins + ".dir"), // remove (now empty) plugins dir
								new XAttribute("On", "both")
							)
						)
					));

					feature.Add(new XElement(ns + "ComponentRef", new XAttribute("Id", componentInstalldirPlugins)));

					var componentEmptyInstalldirPlugins = $"Component.{installdirPlugins}.empty";

					// Add element to create empty plugins folder
					product.Add(new XElement(ns + "DirectoryRef",
						new XAttribute("Id", installdirPlugins),
						new XElement(ns + "Component",
							new XAttribute("Id", componentEmptyInstalldirPlugins),
							new XAttribute("Guid", WixGuid.NewGuid(componentEmptyInstalldirPlugins)),
							new XAttribute("Win64", "yes"),
							new XAttribute("KeyPath", "yes"),
							new XElement(ns + "CreateFolder")
						)
					));

					feature.Add(new XElement(ns + "ComponentRef", new XAttribute("Id", componentEmptyInstalldirPlugins)));
				}
				
				feature.Add(new XElement(ns + "ComponentRef", new XAttribute("Id", componentId)));			
			}

			// include WixFailWhenDeferred Custom Action
			// see http://wixtoolset.org/documentation/manual/v3/customactions/wixfailwhendeferred.html
			product.Add(new XElement(ns + "CustomActionRef", new XAttribute("Id", "WixFailWhenDeferred")));
			
			
			var installExecuteSequence = documentRoot.Descendants(ns + "InstallExecuteSequence").Single();

			// Add condition to InstallServices
			installExecuteSequence.Add(new XElement(ns + "InstallServices",
				// Sequence number pinned from inspecting MSI with Orca
				new XAttribute("Sequence", "5800"),
				new XCData($"VersionNT AND (NOT Installed) " +
				           $"AND ({InstallAsServiceProperty}~=\"true\" OR {InstallAsServiceProperty}=1)")
			));

			// Add condition to StartServices	
			installExecuteSequence.Add(new XElement(ns + "StartServices",
				// Sequence number pinned from inspecting MSI with Orca
				new XAttribute("Sequence", "5900"),
				new XCData($"VersionNT AND (NOT Installed) " +
				           $"AND ({InstallAsServiceProperty}~=\"true\" OR {InstallAsServiceProperty}=1) " +
				           $"AND ({StartAfterInstallProperty}~=\"true\" OR {StartAfterInstallProperty}=1)")
			));

			// Add condition to StopServices to run when 
			// 1. installing
			// 2. uninstalling everything and not part of an upgrade. In this scenario, we assume that the
			//    installation of the new version will have stopped the service as part of the install.
			installExecuteSequence.Add(new XElement(ns + "StopServices",
				// Sequence number pinned from inspecting MSI with Orca
				new XAttribute("Sequence", "1900"),
				new XCData("VersionNT AND ((NOT Installed) OR ((NOT UPGRADINGPRODUCTCODE) AND REMOVE~=\"ALL\"))")
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

		private static Regex WixDuplicateRegex = new Regex(@"Component\.(.+)\.\d+");
		private static Regex WixAbbreviationRegex = new Regex(@"\._\.\.\.(.+)$");
		
		private static void PatchDuplicateComponentId(XNamespace ns, XElement component, string directoryName)
		{
			var idAttribute = component.Attribute("Id");
			if (idAttribute == null) return;

			var dir = directoryName.Replace(".", "_");
			var id = idAttribute.Value;
			idAttribute.Remove();
			var newId = id;
			if (WixAbbreviationRegex.IsMatch(id))
			{
				var componentFile = component.Element(ns + "File")?.Attribute("Source")?.Value ?? throw new Exception($"Component {id} missing file source!");
				var file = Path.GetFileName(componentFile);
				newId = $"{dir}.{file}";
			}
			else if (WixDuplicateRegex.IsMatch(id))
			{
				newId = Regex.Replace(id, @"Component\.(.+)\.\d+", $"{dir}.$1");
			}
			newId = newId.Replace("-", "_");

			// include directory name in Component Id
			var newComponentId = $"Component.{newId}";
			component.Add(new XAttribute("Id", newComponentId));

			// generate a new guid based on the new Component Id
			component.Attribute("Guid").Value = WixGuid.NewGuid(newComponentId).ToString().ToLowerInvariant();

			PatchDuplicateComponentFileId(ns, component, newId);
			PatchComponentRef(ns, component, id, newComponentId);
		}
	}
}
