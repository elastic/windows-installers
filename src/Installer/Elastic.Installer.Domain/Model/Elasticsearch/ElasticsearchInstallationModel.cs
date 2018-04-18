using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch.Closing;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Model.Elasticsearch.Notice;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentValidation.Results;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Elasticsearch
{
	public class ElasticsearchInstallationModel : InstallationModelBase<ElasticsearchInstallationModel, ElasticsearchInstallationModelValidator>
	{
		public JavaConfiguration JavaConfiguration { get; }
		public ElasticsearchEnvironmentConfiguration ElasticsearchEnvironmentConfiguration { get; }
		public TempDirectoryConfiguration TempDirectoryConfiguration { get; }
		private readonly ElasticsearchYamlConfiguration _yamlConfiguration;

		public NoticeModel NoticeModel { get; }
		public LocationsModel LocationsModel { get; }
		public ConfigurationModel ConfigurationModel { get; }
		public PluginsModel PluginsModel { get; }
		public XPackModel XPackModel { get; }
		public ServiceModel ServiceModel { get; }
		public ClosingModel ClosingModel { get; }

		protected override string[] PrerequisiteProperties => base.PrerequisiteProperties.Concat(new[]
			{
				nameof(JavaInstalled),
				nameof(JavaMisconfigured),
				nameof(Using32BitJava),
				nameof(BadElasticsearchYamlFile)
			})
			.ToArray();

		public ElasticsearchInstallationModel(
			IWixStateProvider wixStateProvider, 
			JavaConfiguration javaConfiguration, 
			ElasticsearchEnvironmentConfiguration elasticsearchEnvironmentConfiguration, 
			IServiceStateProvider serviceStateProvider, 
			IPluginStateProvider pluginStateProvider, 
			ElasticsearchYamlConfiguration yamlConfiguration, 
			LocalJvmOptionsConfiguration localJvmOptions, 
			TempDirectoryConfiguration tempDirectoryConfiguration, 
			IFileSystem fileSystem, 
			ISession session, 
			string[] args) : base(wixStateProvider, session, args)
		{
			this.JavaConfiguration = javaConfiguration ?? throw new ArgumentNullException(nameof(javaConfiguration));
			this.ElasticsearchEnvironmentConfiguration = elasticsearchEnvironmentConfiguration;
			this.TempDirectoryConfiguration = tempDirectoryConfiguration;
			this._yamlConfiguration = yamlConfiguration;

			var versionConfig = new VersionConfiguration(wixStateProvider, this.Session.IsInstalled);
			this.SameVersionAlreadyInstalled = versionConfig.SameVersionAlreadyInstalled;
			this.UnInstalling = this.Session.IsUninstalling;
			this.Installing = this.Session.IsInstalling;
			this.Installed = this.Session.IsInstalled;
			this.Upgrading = this.Session.IsUpgrading;
			this.HigherVersionAlreadyInstalled = versionConfig.HigherVersionAlreadyInstalled;

			this.LocationsModel = new LocationsModel(elasticsearchEnvironmentConfiguration, yamlConfiguration, versionConfig, fileSystem);
			this.ServiceModel = new ServiceModel(serviceStateProvider, versionConfig);
			this.NoticeModel = new NoticeModel(versionConfig, serviceStateProvider, this.LocationsModel, this.ServiceModel);
			this.ConfigurationModel = new ConfigurationModel(yamlConfiguration, localJvmOptions);

			var pluginDependencies = this.WhenAnyValue(
				vm => vm.NoticeModel.ExistingVersionInstalled,
				vm => vm.LocationsModel.PreviousInstallationDirectory,
				vm => vm.LocationsModel.ConfigDirectory
			);
			this.PluginsModel = new PluginsModel(pluginStateProvider, pluginDependencies);
			var upgradeFromXPackPlugin = this.WhenAnyValue(vm => vm.PluginsModel.PreviousInstallationHasXPack);
			
			var canAutomaticallySetup = this.WhenAnyValue(vm => vm.ServiceModel.StartAfterInstall, vm => vm.ServiceModel.InstallAsService)
				.Select(t => t.Item1 && t.Item2);
			this.XPackModel = new XPackModel(versionConfig, canAutomaticallySetup, upgradeFromXPackPlugin);

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;
			var observeHost = this.WhenAnyValue(vm => vm.ConfigurationModel.NetworkHost, vm => vm.ConfigurationModel.HttpPort,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeInstallationLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);
			var observeElasticsearchLog = this.WhenAnyValue(vm => vm.LocationsModel.ElasticsearchLog);

			this.ClosingModel = new ClosingModel(wixStateProvider.InstallerVersion, isUpgrade, observeHost, observeInstallationLog, observeElasticsearchLog, serviceStateProvider);
			
			this.AllSteps.AddRange(new List<IStep>
			{
				this.NoticeModel,
				this.LocationsModel,
				this.ServiceModel,
				this.ConfigurationModel,
				this.PluginsModel,
				this.XPackModel,
				this.ClosingModel
			});
			this.AllSteps.ChangeTrackingEnabled = true;

			var observeValidationChanges = this.WhenAny(
				vm => vm.NoticeModel.ValidationFailures,
				vm => vm.LocationsModel.ValidationFailures,
				vm => vm.ConfigurationModel.ValidationFailures,
				vm => vm.PluginsModel.ValidationFailures,
				vm => vm.XPackModel.ValidationFailures,
				vm => vm.ServiceModel.ValidationFailures,
				vm => vm.ClosingModel.ValidationFailures,
				vm => vm.TabSelectedIndex,
				(welcome, locations, configuration, plugins, xpack, service, install, index) =>
				{
					var firstInvalidScreen = this.Steps.FirstOrDefault(s => !s.IsValid) ?? this.ClosingModel;
					return firstInvalidScreen;
				});
			observeValidationChanges
				.Subscribe(firstInvalidStep =>
				{
					this.TabFirstInvalidIndex = this.Steps
						.Select((s, i) => new {s, i = (int?)i})
						.Where(t => !t.s.IsValid)
						.Select(t => t.i)
						.FirstOrDefault();
					this.FirstInvalidStepValidationFailures = firstInvalidStep.ValidationFailures;
				});

			this.WhenAny(
					vm => vm.NoticeModel.IsValid,
					vm => vm.LocationsModel.IsValid,
					vm => vm.ConfigurationModel.IsValid,
					vm => vm.PluginsModel.IsValid,
					vm => vm.XPackModel.IsValid,
					vm => vm.ServiceModel.IsValid,
					vm => vm.ClosingModel.IsValid,
					(welcome, locations, configuration, plugins, xpack, service, install) =>
					{
						var firstInvalidScreen = this.Steps.Select((s, i) => new {s, i}).FirstOrDefault(s => !s.s.IsValid);
						return firstInvalidScreen?.i ?? (this.Steps.Count - 1);
					})
				.Subscribe(selected =>
				{
					this.TabSelectionMax = selected;
					//if one of the steps prior to the current selection is invalid jump back
					if (this.TabSelectedIndex > this.TabSelectionMax)
						this.TabSelectedIndex = this.TabSelectionMax;

					this.FirstInvalidStepValidationFailures = this.ActiveStep.ValidationFailures;
				});

			this.Steps.Changed.Subscribe(e =>
			{
				var firstInvalidScreen = this.Steps.Select((s, i) => new {s, i}).FirstOrDefault(s => !s.s.IsValid);
				var selectedTabIndex = firstInvalidScreen?.i ?? (this.Steps.Count - 1);
				this.TabSelectionMax = selectedTabIndex;
				
				//if one of the steps prior to the current selection is invalid jump back
				if (this.TabSelectedIndex > this.TabSelectionMax)
					this.TabSelectedIndex = this.TabSelectionMax;

				this.FirstInvalidStepValidationFailures = this.ActiveStep.ValidationFailures;

			});

			this.Install = ReactiveCommand.CreateAsyncTask(observeValidationChanges.Select(s => s.IsValid), _ =>
			{
				this.TabSelectedIndex += 1;
				return this.InstallUITask();
			});

			this.Install.Subscribe(installationObservable =>
			{
				installationObservable.Subscribe(installed => this.ClosingModel.Installed = installed);
			});

			this.Refresh();
			//validate the first stab explicitly on constructing this
			//main viewmodel. WPF triggers a validation already
			this.ParsedArguments = new ElasticsearchArgumentParser(
				this.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] {this}).ToList(), args);

			this.ActiveStep.Validate();
		}

		public bool Installing { get; }

		public bool Installed { get; }

		public bool UnInstalling { get; }

		public static ElasticsearchInstallationModel Create(IWixStateProvider wixState, ISession session, params string[] args)
		{
			var javaConfig = JavaConfiguration.Default;
			var esEnvironmentConfig = ElasticsearchEnvironmentConfiguration.Default;
			var serviceState = ServiceStateProvider.FromSession(session, ServiceModel.ElasticsearchServiceName);
			var pluginState = PluginStateProviderBase.ElasticsearchDefault(session);

			var esConfig = ElasticsearchYamlConfiguration.FromFolder(esEnvironmentConfig.ConfigDirectory);
			var jvmConfig = LocalJvmOptionsConfiguration.FromFolder(esEnvironmentConfig.ConfigDirectory);
			var fileSystem = new FileSystem();
			var tempDirConfig = new TempDirectoryConfiguration(session, ElasticsearchEnvironmentStateProvider.Default, fileSystem);
			return new ElasticsearchInstallationModel(wixState, 
				javaConfig, esEnvironmentConfig, serviceState, pluginState, esConfig, jvmConfig, tempDirConfig, fileSystem,
				session, args);
		}

		bool javaInstalled;
		public bool JavaInstalled
		{
			get => javaInstalled;
			set => this.RaiseAndSetIfChanged(ref javaInstalled, value);
		}

		bool javaMisconfigured;
		public bool JavaMisconfigured
		{
			get => javaMisconfigured;
			set => this.RaiseAndSetIfChanged(ref javaMisconfigured, value);
		}

		bool badElasticsearchYamlFile;
		public bool BadElasticsearchYamlFile
		{
			get => badElasticsearchYamlFile;
			set => this.RaiseAndSetIfChanged(ref badElasticsearchYamlFile, value);
		}
		
		bool using32BitJava;
		public bool Using32BitJava
		{
			get => using32BitJava;
			set => this.RaiseAndSetIfChanged(ref using32BitJava, value);
		}

		bool upgrading;
		public bool Upgrading
		{
			get => upgrading;
			set => this.RaiseAndSetIfChanged(ref upgrading, value);
		}

		public override void Refresh()
		{
			foreach (var step in this.AllSteps) step.Refresh();

			this.JavaInstalled = JavaConfiguration.JavaInstalled;
			this.JavaMisconfigured = JavaConfiguration.JavaMisconfigured;
			this.Using32BitJava = JavaConfiguration.Using32BitJava;
			this.BadElasticsearchYamlFile = _yamlConfiguration.FoundButNotValid;
			this.MsiLogFileLocation = this.Session.Get<string>("MsiLogFileLocation");
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(ElasticsearchInstallationModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(ValidationFailures)} = " + ValidationFailuresString(this.ValidationFailures));
			sb.AppendLine($"- {nameof(FirstInvalidStepValidationFailures)} = " + ValidationFailuresString(this.FirstInvalidStepValidationFailures));
			sb.AppendLine($"- {nameof(MsiLogFileLocation)} = " + MsiLogFileLocation);
			sb.AppendLine($"- {nameof(JavaInstalled)} = " + JavaInstalled);
			sb.AppendLine($"- {nameof(JavaMisconfigured)} = " + JavaMisconfigured);
			sb.AppendLine($"- {nameof(Using32BitJava)} = " + Using32BitJava);
			sb.AppendLine($"- {nameof(BadElasticsearchYamlFile)} = " + BadElasticsearchYamlFile);
			sb.AppendLine(this.NoticeModel.ToString());
			sb.AppendLine(this.LocationsModel.ToString());
			sb.AppendLine(this.ServiceModel.ToString());
			sb.AppendLine(this.ConfigurationModel.ToString());
			sb.AppendLine(this.PluginsModel.ToString());
			sb.AppendLine(this.XPackModel.ToString());
			return sb.ToString();
		}

		private string ValidationFailuresString(IEnumerable<ValidationFailure> failures) =>
			failures.Aggregate(new StringBuilder(), (sb, v) =>
				sb.AppendLine($"  • '{v.PropertyName}': {v.ErrorMessage}"), sb => sb.ToString());

		public override string[] HiddenProperties =>
			ModelArgumentParser.GetProperties(this.GetType())		
				.Select(p => p.GetCustomAttribute<ArgumentAttribute>())
				.Where(a => a.IsHidden)
				.Select(a => a.Name)
				.Concat(this.Steps.SelectMany(s => s.HiddenProperties))				
				.ToArray();
	}
}