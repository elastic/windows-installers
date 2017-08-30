using Elastic.Installer.Msi.CustomActions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Elastic.Installer.Domain.Model.Base.Service;
using WixSharp;
using static System.Reflection.Assembly;

namespace Elastic.Installer.Msi
{
	public class Program
	{
		private static bool _releaseMode;
		private static string _productName;
		private static string _productTitle;

		private static void Main(string[] args)
		{
			_productName = args[0].ToLowerInvariant();
			_productTitle = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_productName);

			var product = GetProduct(_productName);
			var version = args[1];
			var distributionRoot = Path.Combine(args[2], $"{_productName}-{version}");

			_releaseMode = args.Length > 3 && !string.IsNullOrEmpty(args[3]);

			// set properties with default values in the MSI so that they 
			// don't have to be set on the command line.
			var setupParams = product.MsiParams;
			var staticProperties = setupParams
				.Where(v => v.Attribute.IsStatic)
				.Select(a => new Property(a.Key, a.Value)
			);

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
					// https://msdn.microsoft.com/en-us/library/aa371182(v=vs.85).aspx
					new Property("REINSTALLMODE", "amus"), 
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
										Dirs = product.Files(distributionRoot, $"{_productName}.exe").ToArray(),
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

			// needed for WixFailWhenDeferred custom action
			if (!_releaseMode)
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

			// see http://www.syntevo.com/blog/?p=3508
			var components = document.Root.Descendants(ns + "Component")
				.Where(c => c.Descendants(ns + "File").Any())
				.Select(c => new { Component = c, File = c.Descendants(ns + "File").First() });

			bool exeFound = false;
			var exeName = $"{_productName}.exe";

			foreach (var component in components)
			{
				var fileId = component.File.Attribute("Id").Value;

				component.Component.AddFirst(
					new XElement(ns + "RemoveFile",
						new XAttribute("Id", fileId),
						new XAttribute("Name", fileId),
						new XAttribute("On", "both")
					)
				);

				// Stop the service when installing.
				// Use a ServiceControl element as opposed to a custom action as an item in the ServiceControl table
				// signals that a service will be stopped as part of the install process, preventing
				// the FileDialog in use window from appearing when upgrading		
				if (fileId == exeName)
				{
					exeFound = true;

					// Remove CompanionFile from exe as it has a version
					//component.File.Attribute("CompanionFile").Remove();

					//component.Component.Add(new XElement(ns + "ServiceControl",
					//	new XAttribute("Id", fileId),
					//	new XAttribute("Name", _productTitle), // MUST match the name of the service
					//	new XAttribute("Stop", "install"),
					//	new XAttribute("Wait", "yes")
					//));
				}
			}

			if (!exeFound) throw new Exception($"No File element found with Id '{_productName}.exe'");

			//var installExecuteSequence = document.Root.Descendants(ns + "InstallExecuteSequence").Single();
			//installExecuteSequence.Add(new XElement(ns + "StopServices",
			//	new XAttribute("Sequence", "1900"),
			//	new XCData($"VersionNT AND (NOT UPGRADINGPRODUCTCODE AND REMOVE=\"ALL\")")
			//));

			// include WixFailWhenDeferred Custom Action when not building a release
			// see http://wixtoolset.org/documentation/manual/v3/customactions/wixfailwhendeferred.html
			if (!_releaseMode)
			{
				var product = document.Root.Descendants(ns + "Product").First();
				product.Add(new XElement(ns + "CustomActionRef",
						new XAttribute("Id", "WixFailWhenDeferred")
					)
				);
			}
		}
	}
}
