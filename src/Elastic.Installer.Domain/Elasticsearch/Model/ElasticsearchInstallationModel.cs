using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Elasticsearch.Model.Closing;
using Elastic.Installer.Domain.Elasticsearch.Model.Config;
using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using Elastic.Installer.Domain.Elasticsearch.Model.Notice;
using Elastic.Installer.Domain.Elasticsearch.Model.Plugins;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Service;
using Elastic.Installer.Domain.Service.Elasticsearch;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Model.Closing;
using Elastic.Installer.Domain.Shared.Model.Plugins;
using Elastic.Installer.Domain.Shared.Model.Service;
using FluentValidation.Results;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Elasticsearch.Model
{
	public class ElasticsearchInstallationModel
		: InstallationModelBase<ElasticsearchInstallationModel, ElasticsearchInstallationModelValidator>
	{
		public JavaConfiguration JavaConfiguration { get; }
		public IElasticsearchEnvironmentStateProvider ElasticsearchEnvironmentState { get; }
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
				nameof(BadElasticsearchYamlFile)
			})
			.ToArray();

		public ElasticsearchInstallationModel(
			IWixStateProvider wixStateProvider,
			JavaConfiguration javaConfiguration,
			IElasticsearchEnvironmentStateProvider environmentStateProvider,
			IServiceStateProvider serviceStateProvider,
			IPluginStateProvider pluginStateProvider,
			ElasticsearchYamlConfiguration yamlConfiguration,
			LocalJvmOptionsConfiguration localJvmOptions,
			ISession session,
			string[] args
		) : base(wixStateProvider, session, args)
		{
			if (javaConfiguration == null) throw new ArgumentNullException(nameof(javaConfiguration));

			this.JavaConfiguration = javaConfiguration;
			this.ElasticsearchEnvironmentState = environmentStateProvider;
			this._yamlConfiguration = yamlConfiguration;

			var versionConfig = new VersionConfiguration(wixStateProvider);
			this.SameVersionAlreadyInstalled = versionConfig.SameVersionAlreadyInstalled;
			this.HigherVersionAlreadyInstalled = versionConfig.HigherVersionAlreadyInstalled;

			this.LocationsModel = new LocationsModel(environmentStateProvider, yamlConfiguration, versionConfig);
			this.NoticeModel = new NoticeModel(versionConfig, serviceStateProvider, this.LocationsModel);
			this.ServiceModel = new ServiceModel(serviceStateProvider, versionConfig);
			this.ConfigurationModel = new ConfigurationModel(yamlConfiguration, localJvmOptions);


			var pluginDependencies = this.WhenAnyValue(
				vm => vm.ConfigurationModel.IngestNode,
				vm => vm.NoticeModel.AlreadyInstalled,
				vm => vm.LocationsModel.InstallDir,
				vm => vm.LocationsModel.ConfigDirectory
			);
			this.PluginsModel = new PluginsModel(pluginStateProvider, pluginDependencies);

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;
			var observeHost = this.WhenAnyValue(vm => vm.ConfigurationModel.NetworkHost, vm => vm.ConfigurationModel.HttpPort,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeInstallationLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);
			var observeElasticsearchLog = this.WhenAnyValue(vm => vm.LocationsModel.ElasticsearchLog);

			this.ClosingModel = new ClosingModel(wixStateProvider.CurrentVersion, isUpgrade, observeHost, observeInstallationLog,
				observeElasticsearchLog, serviceStateProvider);
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
				installationObservable.Subscribe(installed => { this.ClosingModel.Installed = installed; });
			});

			this.Refresh();
			//validate the first stab explicitly on constructing this
			//main viewmodel. WPF triggers a validation already
			this.ParsedArguments = new ElasticsearchArgumentParser(
				this.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] {this}).ToList(), args);

			this.ActiveStep.Validate();
		}

		public static ElasticsearchInstallationModel Create(IWixStateProvider wixState, ISession session,
			params string[] args)
		{
			var javaConfig = JavaConfiguration.Default;
			var esState = ElasticsearchEnvironmentStateProvider.Default;
			var serviceState = ServiceStateProvider.FromSession(session, "Elasticsearch");
			var pluginState = PluginStateProvider.ElasticsearchDefault(session);

			var esConfig = ElasticsearchYamlConfiguration.FromFolder(esState.ConfigDirectory);
			var jvmConfig = LocalJvmOptionsConfiguration.FromFolder(esState.ConfigDirectory);
			return new ElasticsearchInstallationModel(wixState, javaConfig, esState, serviceState, pluginState, esConfig,
				jvmConfig, session, args);
		}

		bool javaInstalled;

		public bool JavaInstalled
		{
			get { return javaInstalled; }
			set { this.RaiseAndSetIfChanged(ref javaInstalled, value); }
		}

		bool javaMisconfigured;

		public bool JavaMisconfigured
		{
			get { return javaMisconfigured; }
			set { this.RaiseAndSetIfChanged(ref javaMisconfigured, value); }
		}

		bool badElasticsearchYamlFile;

		public bool BadElasticsearchYamlFile
		{
			get { return badElasticsearchYamlFile; }
			set { this.RaiseAndSetIfChanged(ref badElasticsearchYamlFile, value); }
		}

		public override void Refresh()
		{
			foreach (var step in this.Steps) step.Refresh();

			this.JavaInstalled = JavaConfiguration.JavaInstalled;
			this.JavaMisconfigured = JavaConfiguration.JavaMisconfigured;
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