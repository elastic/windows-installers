using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Service;
using FluentValidation.Results;
using ReactiveUI;
using Elastic.Installer.Domain.Session;
using System.Reactive.Linq;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Elasticsearch.Model.Notice;
using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using Elastic.Installer.Domain.Elasticsearch.Model.Config;
using Elastic.Installer.Domain.Elasticsearch.Model.Plugins;
using Elastic.Installer.Domain.Elasticsearch.Model.Service;
using Elastic.Installer.Domain.Elasticsearch.Model.Closing;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Elasticsearch.Configuration;

namespace Elastic.Installer.Domain.Elasticsearch.Model
{
	public class InstallationModel : ValidatableReactiveObjectBase<InstallationModel, InstallationModelValidator>
	{
		private readonly IWixStateProvider _wixStateProvider;
		public JavaConfiguration JavaConfiguration { get; }
		public IElasticsearchEnvironmentStateProvider ElasticsearchEnvironmentState { get; }
		private readonly ElasticsearchYamlConfiguration _yamlConfiguration;

		public NoticeModel NoticeModel { get; }
		public LocationsModel LocationsModel { get; }
		public ConfigurationModel ConfigurationModel { get; }
		public PluginsModel PluginsModel { get; }
		public ServiceModel ServiceModel { get; }
		public ClosingModel ClosingModel { get; }

		public string ToMsiParamsString() => this.ParsedArguments.ToMsiParamsString();

		public IEnumerable<ModelArgument> ToMsiParams() => this.ParsedArguments.ToMsiParams();

		public ReactiveList<IStep> AllSteps { get; } = new ReactiveList<IStep>();
		public IReactiveDerivedList<IStep> Steps { get; } = new ReactiveList<IStep>().CreateDerivedCollection(x => x, x => true);

		public IValidatableReactiveObject ActiveStep => this.Steps[this.TabSelectedIndex];

		public ModelArgumentParser ParsedArguments { get; }

		public ISession Session { get; }

		public static InstallationModel Create(IWixStateProvider wixState, ISession session, params string[] args)
		{
			var javaConfig = JavaConfiguration.Default;
			var esState = ElasticsearchEnvironmentStateProvider.Default;
			var serviceState = ServiceStateProvider.FromSession(session);
			var pluginState = PluginStateProvider.Default;

			var esConfig = ElasticsearchYamlConfiguration.FromFolder(esState.ConfigDirectory);
			var jvmConfig = LocalJvmOptionsConfiguration.FromFolder(esState.ConfigDirectory);
			return new InstallationModel(wixState, javaConfig, esState, serviceState, pluginState, esConfig, jvmConfig, session, args);
		}

		public InstallationModel(
			IWixStateProvider wixStateProvider,
			JavaConfiguration javaConfiguration,
			IElasticsearchEnvironmentStateProvider environmentStateProvider,
			IServiceStateProvider serviceStateProvider,
			IPluginStateProvider pluginStateProvider,
			ElasticsearchYamlConfiguration yamlConfiguration,
			LocalJvmOptionsConfiguration localJvmOptions,
			ISession session,
			string[] args
			)
		{
			this.Session = session;

			if (wixStateProvider == null) throw new ArgumentNullException(nameof(wixStateProvider));
			if (javaConfiguration == null) throw new ArgumentNullException(nameof(javaConfiguration));

			this._wixStateProvider = wixStateProvider;
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

			var observeHost = this.WhenAnyValue(vm => vm.ConfigurationModel.NetworkHost, vm => vm.ConfigurationModel.HttpPort,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);
			var observeElasticsearchLog = this.WhenAnyValue(vm => vm.LocationsModel.ElasticsearchLog);

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;

			this.ClosingModel = new ClosingModel(wixStateProvider.CurrentVersion, isUpgrade, observeHost, observeLog, observeElasticsearchLog, serviceStateProvider);

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

			this.NextButtonText = TextResources.SetupView_NextText;

			var canMoveForwards = this.WhenAny(vm => vm.TabSelectedIndex, vm => vm.TabSelectionMax,
				(i, max) => i.GetValue() < max.GetValue());

			this.Next = ReactiveCommand.Create(canMoveForwards);
			this.Next.Subscribe(i =>
			{
				this.TabSelectedIndex = Math.Min(this.Steps.Count - 1, this.TabSelectedIndex + 1);
			});

			var canMoveBackwards = this.WhenAny(vm => vm.TabSelectedIndex, (i) => i.GetValue() > 0);
			this.Back = ReactiveCommand.Create(canMoveBackwards);
			this.Back.Subscribe(i =>
			{
				this.TabSelectedIndex = Math.Max(0, this.TabSelectedIndex - 1);
			});

			this.Help = ReactiveCommand.Create();
			this.ShowLicenseBlurb = ReactiveCommand.Create();
			this.ShowCurrentStepErrors = ReactiveCommand.Create();
			this.RefreshCurrentStep = ReactiveCommand.Create();
			this.RefreshCurrentStep.Subscribe(x => { this.Steps[this.TabSelectedIndex].Refresh(); });
			this.Exit = ReactiveCommand.Create();

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
			var canInstall = observeValidationChanges.Select(s => s.IsValid);
			this.Install = ReactiveCommand.CreateAsyncTask(canInstall, _ =>
			{
				this.TabSelectedIndex += 1;
				return this.InstallUITask();
			});

			this.Install.Subscribe(installationObservable =>
			{
				installationObservable.Subscribe(installed =>
				{
					this.ClosingModel.Installed = installed;
				});
			});


			this.WhenAny(vm => vm.TabSelectedIndex, v => v.GetValue())
				.Subscribe(i =>
				{
					var c = this.Steps.Count;
					if (i == (c - 1)) this.NextButtonText = TextResources.SetupView_ExitText;
					else if (i == (c - 2)) this.NextButtonText = TextResources.SetupView_InstallText;
					else this.NextButtonText = TextResources.SetupView_NextText;
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
					var firstInvalidScreen = this.Steps.Select((s, i) => new { s, i }).FirstOrDefault(s => !s.s.IsValid);
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

			this.WhenAnyValue(view => view.ValidationFailures)
				.Subscribe(failures =>
				{
					this.PrequisiteFailures = (failures ?? Enumerable.Empty<ValidationFailure>())
						.Where(v => _prerequisiteProperties.Contains(v.PropertyName))
						.ToList();
				});

			this.Refresh();
			//validate the first stab explicitly on constructing this 
			//main viewmodel. WPF triggers a validation already
			this.ParsedArguments = new InstallationModelArgumentParser(this.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] { this }).ToList(), args);

			this.ActiveStep.Validate();
		}

		public ReactiveCommand<object> Next { get; }
		public ReactiveCommand<object> Back { get; }
		public ReactiveCommand<object> Help { get; set; }
		public ReactiveCommand<object> RefreshCurrentStep { get; }
		public ReactiveCommand<object> ShowCurrentStepErrors { get; set; }
		public ReactiveCommand<object> ShowLicenseBlurb { get; set; }

		public ReactiveCommand<object> Exit { get; private set; }
		public ReactiveCommand<IObservable<ClosingResult>> Install { get; }
		public Func<Task<IObservable<ClosingResult>>> InstallUITask { get; set; }

		public override void Refresh()
		{
			foreach (var step in this.Steps) step.Refresh();

			this.JavaInstalled = JavaConfiguration.JavaInstalled;
			this.JavaMisconfigured = JavaConfiguration.JavaMisconfigured;
			this.BadElasticsearchYamlFile = _yamlConfiguration.FoundButNotValid;

			this.MsiLogFileLocation = this.Session.Get<string>("MsiLogFileLocation");
		}

		protected override bool SkipValidationFor(string propertyName) => propertyName == nameof(PrequisiteFailures);

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
				ElasticsearchHomeDirectory = this.LocationsModel.InstallDir,
				ElasticsearchConfigDirectory = this.LocationsModel.ConfigDirectory,
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

		// ReactiveUI conventions do not change
		// ReSharper disable InconsistentNaming
		// ReSharper disable ArrangeTypeMemberModifiers

		string msiLogFileLocation;
		public string MsiLogFileLocation 
		{
			get { return msiLogFileLocation; }
			set { this.RaiseAndSetIfChanged(ref msiLogFileLocation, value); }
		}

		int tabSelectionMax;
		public int TabSelectionMax
		{
			get { return tabSelectionMax; }
			private set { this.RaiseAndSetIfChanged(ref tabSelectionMax, value); }
		}

		int tabSelectedIndex;
		public int TabSelectedIndex
		{
			get { return tabSelectedIndex; }
			set { this.RaiseAndSetIfChanged(ref tabSelectedIndex, value); }
		}

		string nextButtonText;
		public string NextButtonText
		{
			get { return nextButtonText; }
			private set { this.RaiseAndSetIfChanged(ref nextButtonText, value); }
		}

		//prequisites

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

		bool sameVersionAlreadyInstalled;
		public bool SameVersionAlreadyInstalled
		{
			get { return sameVersionAlreadyInstalled; }
			set { this.RaiseAndSetIfChanged(ref sameVersionAlreadyInstalled, value); }
		}

		bool higherVersionAlreadyInstalled;
		public bool HigherVersionAlreadyInstalled
		{
			get { return higherVersionAlreadyInstalled; }
			set { this.RaiseAndSetIfChanged(ref higherVersionAlreadyInstalled, value); }
		}

		private IList<ValidationFailure> currentValidationFailures = new List<ValidationFailure>();
		public IList<ValidationFailure> CurrentStepValidationFailures
		{
			get { return currentValidationFailures; }
			private set { this.RaiseAndSetIfChanged(ref currentValidationFailures, value); }
		}

		private readonly string[] _prerequisiteProperties = new[]
		{
			nameof(JavaInstalled),
			nameof(JavaMisconfigured),
			nameof(BadElasticsearchYamlFile),
			nameof(SameVersionAlreadyInstalled),
			nameof(HigherVersionAlreadyInstalled)
		};

		private IList<ValidationFailure> prequisiteFailures = new List<ValidationFailure>();

		/// <summary>
		///Prequisite failures are special in that they can only be fixed by user intervention and reinstantiating the viewmodel
		///e.g closing and restarting the installer. They will cause a model dialog to appear with the only option to close the installer
		/// </summary>
		public IList<ValidationFailure> PrequisiteFailures
		{
			get { return prequisiteFailures; }
			private set { this.RaiseAndSetIfChanged(ref prequisiteFailures, value); }
		}


		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(InstallationModel));
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
