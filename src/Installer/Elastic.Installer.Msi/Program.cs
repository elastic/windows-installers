using Elastic.Installer.Msi.CustomActions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WixSharp;
using static System.Reflection.Assembly;

namespace Elastic.Installer.Msi
{
	public class Program
	{
		private static string _productName;
		private static string _productTitle;

		private static void Main(string[] args)
		{
			_productName = args[0].ToLowerInvariant();
			_productTitle = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_productName);

			var version = args[1];
			var product = GetProduct(_productName);
			var distributionRoot = Path.Combine(args[2], $"{_productName}-{version}");

			// set properties with default values in the MSI so that they 
			// don't have to be set on the command line.
			var setupParams = product.MsiParams;
			var staticProperties = setupParams
				.Where(v => v.Attribute.IsStatic || v.Attribute.IsHidden || v.Attribute.IsSecure)
				.Select(a =>
				{
					var property = new Property(a.Key, a.Value) { Attributes = new Attributes() };
					if (a.Attribute.IsHidden)
						property.Attributes.Add("Hidden", "yes");
					if (a.Attribute.IsSecure)
						property.Attributes.Add("Secure", "yes");
					return property;
				});

			// Only set dynamic property if MSI is not installed and value is empty.
			// Dynamic properties calculate the default value at MSI install time
			var dynamicProperties = setupParams
				.Where(v => v.Attribute.IsDynamic)
				.Select(a => new SetPropertyAction(
					a.Key,
					a.Attribute.DynamicValue,
					Return.check,
					When.After,
					Step.PreviousActionOrInstallInitialize,
					new Condition($"(NOT Installed) AND ({a.Key}=\"\")")
				)
			);

			var project = new Project
			{
				ProductId = product.ProductCode[version],
				UpgradeCode = product.UpgradeCode,
				GUID = product.UpgradeCode,
				ControlPanelInfo = new ProductInfo
				{
					Manufacturer = "Elastic",
					ProductIcon = $@"Resources\{_productName}.ico",
					// Shows up as Support Link in Programs and Features
					UrlInfoAbout = $"https://www.elastic.co/products/{_productName}",
					// Shows up as Help Link in Programs and Features
					HelpLink = $"https://discuss.elastic.co/c/{_productName}"
				},
				Name = $"{_productTitle} {version}",
				OutFileName = _productName,
				Version = new Version(version.Split('-')[0]),
				Actions = dynamicProperties
					.Concat(GetExecutingAssembly().GetTypes()
					.Where(t => t != typeof(CustomAction) && typeof(CustomAction).IsAssignableFrom(t) && !t.IsAbstract)
					.Select(t => (CustomAction)Activator.CreateInstance(t))
					.Where(c => c.ProductType == product.GetType())
					.OrderBy(c => c.Order)
					.Select(c => c.ToManagedAction())
					.ToArray<WixSharp.Action>()).ToArray(),
				DefaultFeature = new Feature("Complete", true, false),
				Platform = Platform.x64,
				InstallScope = InstallScope.perMachine,
				Properties = new[]
				{
					// used by the embedded UI to create the correct installation model
					new Property("ElasticProduct", _productName),
					// make it easy to reference current version within MSI process
					new Property("CurrentVersion", version),
					new Property("MsiLogging", "voicewarmup"),
					// do not give option to repair installation
					new Property("ARPNOREPAIR", "yes"),
					// do not give option to change installation
					new Property("ARPNOMODIFY", "yes"),
					// add .NET Framework 4.5 as a dependency
					new PropertyRef("NETFRAMEWORK45"),
				}.Concat(staticProperties).ToArray(),
				LaunchConditions = new List<LaunchCondition>
				{
					new LaunchCondition(
						"Installed OR NETFRAMEWORK45",
						"This installer requires at least .NET Framework 4.5 in order to run custom install actions. " +
						"Please install .NET Framework 4.5 then run this installer again."
					)
				},
				Dirs = new[]
				{
					new Dir(@"ProgramFiles64Folder")
					{
						Dirs = new []
						{
							new Dir(new Id("Elastic"), "Elastic")
							{
								Dirs = new []
								{
									new Dir(new Id("INSTALLDIR"), _productTitle)
									{
										DirFileCollections = new []
										{
											new DirFiles(distributionRoot + @"\*.*")
										},
										Dirs = product.Files(distributionRoot).ToArray(),
									}
								}
							}
						}
					}
				}
			};

			if (product.EmbeddedUI != null)
				project.EmbeddedUI = new EmbeddedAssembly(product.EmbeddedUI.Assembly.Location);
			else
				project.UI = WUI.WixUI_ProgressOnly;

			project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
			project.WixSourceGenerated += PatchWixSource;
			project.IncludeWixExtension(WixExtension.NetFx);
			project.IncludeWixExtension(WixExtension.Util);
			const string wixLocation = @"..\..\..\packages\WixSharp.wix.bin\tools\bin";
			if (!Directory.Exists(wixLocation))
				throw new Exception($"The directory '{wixLocation}' could not be found");
			Compiler.WixLocation = wixLocation;
			Compiler.BuildWxs(project, $@"_Generated\{_productName.ToLower()}.wxs", Compiler.OutputType.MSI);
			Compiler.BuildMsi(project);
		}

		private static Product GetProduct(string name)
		{
			switch (name.ToLower())
			{
				case "elasticsearch":
					return new Elasticsearch.Elasticsearch();
				case "kibana":
					return new Kibana.Kibana();
				default:
					throw new ArgumentException($"Unknown product: {name}");
			}
		}

		private static void PatchWixSource(XDocument document)
		{
			var ns = document.Root.Name.Namespace;

			var directories = document.Root.Descendants(ns + "Directory")
				.Where(c =>
				{
					var childComponents = c.Elements(ns + "Component");
					return childComponents.Any() && childComponents.Any(cc => cc.Descendants(ns + "File").Any());
				});

			var feature = document.Root.Descendants(ns + "Feature").Single();
			foreach (var directory in directories)
			{
				var directoryId = directory.Attribute("Id").Value;
				var componentId = "Component." + directoryId;
				directory.AddFirst(new XElement(ns + "Component",
					new XAttribute("Id", componentId),
					new XAttribute("Guid", WixGuid.NewGuid()),
					new XAttribute("Win64", "yes"),
					new XElement(ns + "RemoveFile",
						new XAttribute("Id", directoryId),
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
							new XAttribute("Guid", WixGuid.NewGuid()),
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

			var exeName = $"{_productName}.exe";
			var exeComponent = document.Root.Descendants(ns + "Component")
				.Where(c => c.Descendants(ns + "File").Any(f => f.Attribute("Id").Value == exeName))
				.Select(c => new { Component = c, File = c.Descendants(ns + "File").First() })
				.SingleOrDefault();

			if (exeComponent == null)
				throw new Exception($"No File element found with Id '{exeName}'");

			var fileId = exeComponent.File.Attribute("Id").Value;
			exeComponent.Component.Add(new XElement(ns + "ServiceControl",
				new XAttribute("Id", fileId),
				new XAttribute("Name", _productTitle), // MUST match the name of the service
				new XAttribute("Stop", "both"),
				new XAttribute("Wait", "yes")
			));

			// include WixFailWhenDeferred Custom Action
			// see http://wixtoolset.org/documentation/manual/v3/customactions/wixfailwhendeferred.html
			var product = document.Root.Descendants(ns + "Product").First();
			product.Add(new XElement(ns + "CustomActionRef",
					new XAttribute("Id", "WixFailWhenDeferred")
				)
			);

			// Update in-built progress templates 
			// See http://web.mit.edu/ops/services/afs/openafs-1.4.1/src/src/WINNT/install/wix/lang/en_US/ActionText.wxi
			var ui = document.Root.Descendants(ns + "UI").Single();
			ui.Add(new XElement(ns + "ProgressText",
				new XAttribute("Action", "InstallFiles"),
				new XAttribute("Template", "Copying new files: [9][1]"),
				new XText("Copying new files")
			), new XElement(ns + "ProgressText",
				new XAttribute("Action", "StopServices"),
				new XAttribute("Template", $"Stopping {_productTitle} service"),
				new XText($"Stopping {_productTitle} service") // TODO: default template doesn't receive service name. Might be because it's not installed with ServiceInstall table?
			));
		}
	}
}
