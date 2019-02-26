using Elastic.Installer.Msi.CustomActions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using WixSharp;
using static System.Reflection.Assembly;

namespace Elastic.Installer.Msi
{
	public class Program
	{
		private const string InstallDir = "INSTALLDIR";
		private static string _productName;
		private static string _productTitle;

		private static void Main(string[] args)
		{
			_productName = args[0].ToLowerInvariant();
			_productTitle = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(_productName);

			var version = args[1];
			var product = GetProduct(_productName);
			var distributionRoot = args[2];

			// set properties with default values in the MSI so that they 
			// don't have to be set on the command line.
			var staticProperties = product.MsiParams
				.Where(v => 
					v.Attribute.IsStatic && !string.IsNullOrEmpty(v.Value) || 
					v.Attribute.IsHidden || 
					v.Attribute.IsSecure || 
					v.Attribute.PersistInRegistry)
				.Select(a =>
				{
					var property = a.Attribute.PersistInRegistry 
						? new RegValueProperty(
							a.Key, 
							RegistryHive.LocalMachine, 
							product.RegistryKey, 
							a.Key, 
							a.Attribute.IsDynamic || a.Key == InstallDir ? null : a.Value)
						{
							Attributes = new Attributes(),
							Win64 = true						
						} 
						: new Property(a.Key, a.Value) { Attributes = new Attributes() };

					if (a.Attribute.IsHidden)
						property.Attributes.Add("Hidden", "yes");
					if (a.Attribute.IsSecure)
						property.Attributes.Add("Secure", "yes");
					return property;
				});

			// Only set dynamic property if MSI is not installed and value is empty.
			// Dynamic properties calculate the default value at MSI install time
			var dynamicProperties = product.MsiParams
				.Where(v => v.Attribute.IsDynamic)
				.Select(a => new SetPropertyAction(
					new Id($"Set_{a.Key}_Value"),
					a.Key,
					a.Attribute.DynamicValue,
					Return.check,
					When.After,
					Step.PreviousActionOrInstallInitialize,
					new Condition($"(NOT Installed) AND ({a.Key}=\"\")")
				)
			);

			// http://robmensching.com/blog/posts/2010/5/2/the-wix-toolsets-remember-property-pattern/
			// Save property values passed from command line, allow values to be set from
			// Registry, and set property values from command line after.
			var propertiesFromCommandline = product.MsiParams
				.Where(v => v.Attribute.PersistInRegistry)
				.SelectMany(v => new[]
				{
					new SetPropertyAction(
						new Id($"Save_{v.Key}_CmdValue"),
						$"Cmd_{v.Key}",
						$"[{v.Key}]"					
					)
					{
						Execute = Execute.firstSequence,
						Step = Step.AppSearch,
						When = When.Before
					},
					new SetPropertyAction(
						new Id($"Set_{v.Key}_CmdValue"),
						v.Key,
						$"[Cmd_{v.Key}]"
					)
					{
						Execute = Execute.firstSequence,
						Step = Step.AppSearch,
						When = When.After
					},
				});

			var project = new Project
			{
				EnvironmentVariables = product.EnvironmentVariables,
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
					.Concat(propertiesFromCommandline)
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
					new Property("ElasticProduct", _productTitle),
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
									new Dir(new Id("Product"), _productTitle)
									{
										Dirs = new[]
										{
											new Dir(new Id(InstallDir), version)
											{
												DirFileCollections = new[]
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
					}
				}
			};

			if (product.EmbeddedUI != null)
				project.EmbeddedUI = new EmbeddedAssembly(product.EmbeddedUI.Assembly.Location);
			else
				project.UI = WUI.WixUI_ProgressOnly;

			project.MajorUpgradeStrategy = MajorUpgradeStrategy.Default;
			project.WixSourceGenerated += product.PatchWixSource;
			project.IncludeWixExtension(WixExtension.NetFx);
			project.IncludeWixExtension(WixExtension.Util);
			product.PatchProject(project);

			const string wixLocation = @"..\..\..\packages\WixSharp.wix.bin\tools\bin";
			if (!Directory.Exists(wixLocation))
				throw new Exception($"The directory '{wixLocation}' could not be found");
			//Compiler.LightOptions = "-sw1076 -sw1079 -sval";
			Compiler.WixLocation = wixLocation;
			Compiler.BuildWxs(project, $@"_Generated\{_productName.ToLower()}.wxs", Compiler.OutputType.MSI);
			Compiler.BuildMsi(project);
		}

		private static Product GetProduct(string name)
		{
			switch (name.ToLowerInvariant())
			{
				case "elasticsearch":
					return new Elasticsearch.Elasticsearch();
				case "kibana":
					return new Kibana.Kibana();
				default:
					throw new ArgumentException($"Unknown product: {name}");
			}
		}
	}
}
