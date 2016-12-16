using Elastic.Installer.Domain.Session;
using Elastic.Installer.Msi.CustomActions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using WixSharp;
using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.Msi.Elasticsearch;
using Elastic.Installer.Msi.Kibana;

namespace Elastic.Installer.Msi
{
	class Program
	{
		static void Main(string[] args)
		{
			var productName = args[0].ToLower();
			var product = GetProduct(productName);
			var version = args[1];
			var distributionRoot = Path.Combine(args[2], $"{productName}-{version}"); ;


			// set properties in the MSI so that they don't have to be set on the command line.
			// The only awkward one is plugins as it has a not empty default value, but an empty
			// string can be passed to not install any plugins
			var model = InstallationModel.Create(new NoopWixStateProvider(), new NoopSession());
			var setupParams = model.ToMsiParams();
			var staticProperties = setupParams
				.Where(v => v.Attribute.IsStatic)
				.Select(a => new Property(a.Key, a.Value)
			);

			// Only set dynamic property if MSI is not installed and value is empty 
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
					ProductIcon = $@"Resources\{productName}.ico",
					// Shows up as Support Link in Programs and Features
					UrlInfoAbout = $"https://www.elastic.co/products/{productName}",
					// Shows up as Help Link in Programs and Features
					HelpLink = $"https://discuss.elastic.co/c/{productName}"
				},
				Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase($"{productName} ") + version,
				OutFileName = productName,
				Version = new Version(version.Split('-')[0]),
				Actions = dynamicProperties
					.Concat(System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
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
					// make it easy to reference current version within MSI process
					new Property("CurrentVersion", version),
					new Property("MsiLogging", "voicewarmup"),
					// do not give option to repair installation
					new Property("ARPNOREPAIR", "yes"),
					// do not give option to change installation
					new Property("ARPNOMODIFY", "yes"),
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
									new Dir(new Id("INSTALLDIR"), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(productName))
									{
										DirFileCollections = new []
										{
											new DirFiles(distributionRoot + @"\*.*")
										},
										Dirs = product.Files(distributionRoot).ToArray()
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
			project.MajorUpgradeStrategy.RemoveExistingProductAfter = Step.InstallValidate;
			project.WixSourceGenerated += PatchWixSource;
			project.IncludeWixExtension(WixExtension.NetFx);

			Compiler.BuildWxs(project, $@"_Generated\{productName.ToLower()}.wxs", Compiler.OutputType.MSI);
			Compiler.BuildMsi(project);
		}

		static Product GetProduct(string name)
		{
			switch (name.ToLower())
			{
				case "elasticsearch":
					return new ElasticsearchProduct();
				case "kibana":
					return new KibanaProduct();
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

			foreach (var component in components)
			{
				component.Component.AddFirst(
					new XElement(ns + "RemoveFile",
						new XAttribute("Id", component.File.Attribute("Id").Value),
						new XAttribute("Name", component.File.Attribute("Id").Value),
						new XAttribute("On", "both")
					)
				);
			}
		}
	}
}
