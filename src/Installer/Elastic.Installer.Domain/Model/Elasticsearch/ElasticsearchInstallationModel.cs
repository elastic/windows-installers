using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceProcess;
using System.Text;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch.Closing;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Model.Elasticsearch.Notice;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using FluentValidation.Results;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Elasticsearch
{
	public class ElasticsearchInstallationModel
		: InstallationModelBase<ElasticsearchInstallationModel, ElasticsearchInstallationModelValidator>
	{
		public JavaConfiguration JavaConfiguration { get; }
		public ElasticsearchEnvironmentConfiguration ElasticsearchEnvironmentConfiguration { get; }
		private readonly ElasticsearchYamlConfiguration _yamlConfiguration;

		public NoticeModel NoticeModel { get; }
		public LocationsModel LocationsModel { get; }
		public ConfigurationModel ConfigurationModel { get; }
		public PluginsModel PluginsModel { get; }
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
			ISession session,
			string[] args
		) : base(wixStateProvider, session, args)
		{
			this.JavaConfiguration = javaConfiguration ?? throw new ArgumentNullException(nameof(javaConfiguration));
			this.ElasticsearchEnvironmentConfiguration = elasticsearchEnvironmentConfiguration;
			this._yamlConfiguration = yamlConfiguration;

			var versionConfig = new VersionConfiguration(wixStateProvider, this.Session.IsInstalled);
			this.SameVersionAlreadyInstalled = versionConfig.SameVersionAlreadyInstalled;
			this.UnInstalling = this.Session.IsUninstalling;
			this.Installing = this.Session.IsInstalling;
			this.Installed = this.Session.IsInstalled;
			this.Upgrading = this.Session.IsUpgrading;
			this.HigherVersionAlreadyInstalled = versionConfig.HigherVersionAlreadyInstalled;

			this.LocationsModel = new LocationsModel(elasticsearchEnvironmentConfiguration, yamlConfiguration, versionConfig);
			this.NoticeModel = new NoticeModel(versionConfig, serviceStateProvider, this.LocationsModel);
			this.ServiceModel = new ServiceModel(serviceStateProvider, versionConfig);
			this.ConfigurationModel = new ConfigurationModel(yamlConfiguration, localJvmOptions);

			var pluginDependencies = this.WhenAnyValue(
				vm => vm.NoticeModel.ExistingVersionInstalled,
				vm => vm.LocationsModel.InstallDir,
				vm => vm.LocationsModel.ConfigDirectory
			);
			this.PluginsModel = new PluginsModel(pluginStateProvider, pluginDependencies);

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;
			var observeHost = this.WhenAnyValue(vm => vm.ConfigurationModel.NetworkHost, vm => vm.ConfigurationModel.HttpPort,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeInstallationLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);
			var observeElasticsearchLog = this.WhenAnyValue(vm => vm.LocationsModel.ElasticsearchLog);

			var installXPack = this.PluginsModel.DefaultPlugins().Contains("x-pack");
			var observeInstallXPack = this.PluginsModel.AvailablePlugins.ItemChanged
				.Where(x => x.PropertyName == nameof(Plugin.Selected) && x.Sender.PluginType == PluginType.XPack)
				.Select(x => x.Sender.Selected);

			this.ClosingModel = new ClosingModel(wixStateProvider.CurrentVersion, isUpgrade, installXPack, observeHost, observeInstallationLog,
				observeElasticsearchLog, observeInstallXPack, serviceStateProvider);
			this.AllSteps = new ReactiveList<IStep>
			{
				this.NoticeModel,
				this.LocationsModel,
				this.ServiceModel,
				this.ConfigurationModel,
				this.PluginsModel,
				this.ClosingModel
			};
			this.Steps = this.AllSteps.CreateDerivedCollection(x => x, x => x.IsRelevant);

			var observeValidationChanges = this.WhenAny(
				vm => vm.NoticeModel.ValidationFailures,
				vm => vm.LocationsModel.ValidationFailures,
				vm => vm.ConfigurationModel.ValidationFailures,
				vm => vm.PluginsModel.ValidationFailures,
				vm => vm.ServiceModel.ValidationFailures,
				vm => vm.ClosingModel.ValidationFailures,
				vm => vm.TabSelectedIndex,
				(welcome, locations, configuration, plugins, service, install, index) =>
				{
					var firstInvalidScreen = this.Steps.FirstOrDefault(s => !s.IsValid) ?? this.ClosingModel;
					return firstInvalidScreen;
				});
			observeValidationChanges
				.Subscribe(selected =>
				{
					var step = this.Steps[this.TabSelectedIndex];
					var failures = step.ValidationFailures;
					this.CurrentStepValidationFailures = selected.ValidationFailures;
				});

			this.WhenAny(
					vm => vm.NoticeModel.IsValid,
					vm => vm.LocationsModel.IsValid,
					vm => vm.ConfigurationModel.IsValid,
					vm => vm.PluginsModel.IsValid,
					vm => vm.ServiceModel.IsValid,
					vm => vm.ClosingModel.IsValid,
					(welcome, locations, configuration, plugins, service, install) =>
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

					this.CurrentStepValidationFailures = this.ActiveStep.ValidationFailures;
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
			var serviceState = ServiceStateProvider.FromSession(session, "Elasticsearch");
			var pluginState = PluginStateProviderBase.ElasticsearchDefault(session);

			var esConfig = ElasticsearchYamlConfiguration.FromFolder(esEnvironmentConfig.ConfigDirectory);
			var jvmConfig = LocalJvmOptionsConfiguration.FromFolder(esEnvironmentConfig.ConfigDirectory);
			return new ElasticsearchInstallationModel(wixState, javaConfig, esEnvironmentConfig, serviceState, pluginState, esConfig,
				jvmConfig, session, args);
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
			foreach (var step in this.Steps) step.Refresh();

			this.JavaInstalled = JavaConfiguration.JavaInstalled;
			this.JavaMisconfigured = JavaConfiguration.JavaMisconfigured;
			this.Using32BitJava = JavaConfiguration.Using32BitJava;
			this.BadElasticsearchYamlFile = _yamlConfiguration.FoundButNotValid;

			this.MsiLogFileLocation = this.Session.Get<string>("MsiLogFileLocation");
		}

		public ElasticsearchServiceConfiguration GetServiceConfiguration()
		{
			var service = this.ServiceModel;
			var automaticStart = service.StartWhenWindowsStarts;
			var configuration = new ElasticsearchServiceConfiguration
			{
				Name = "Elasticsearch",
				DisplayName = "Elasticsearch",
				Description = "You know, for Search.",
				StartMode = automaticStart ? ServiceStartMode.Automatic : ServiceStartMode.Manual,
				EventLogSource = "Elasticsearch",
				HomeDirectory = this.LocationsModel.InstallDir,
				ConfigDirectory = this.LocationsModel.ConfigDirectory,
				ExeLocation = Path.Combine(this.LocationsModel.InstallDir, "bin", "elasticsearch.exe")
			};
			var username = service.User;

			var password = service.Password;
			if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
			{
				configuration.ServiceAccount = ServiceAccount.User;
				configuration.UserName = username;
				configuration.Password = password;
			}
			else if (service.UseNetworkService)
				configuration.ServiceAccount = ServiceAccount.NetworkService;
			else
				configuration.ServiceAccount = ServiceAccount.LocalSystem;
			return configuration;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(ElasticsearchInstallationModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(ValidationFailures)} = " + ValidationFailuresString(this.ValidationFailures));
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
			return sb.ToString();
		}

		private string ValidationFailuresString(IEnumerable<ValidationFailure> failures) =>
			failures.Aggregate(new StringBuilder(), (sb, v) =>
				sb.AppendLine($"  • '{v.PropertyName}': {v.ErrorMessage}"), sb => sb.ToString());
	}
}