using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Kibana.Model.Closing;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Properties;
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
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Model
{
	public class InstallationModel 
		: ValidatableReactiveObjectBase<InstallationModel, InstallationModelValidator>
	{
		public ServiceModel ServiceModel { get; }
		public PluginsModel PluginsModel { get; }
		public ClosingModel ClosingModel { get; }

		// Move to base class
		private readonly IWixStateProvider _wixStateProvider;
		public ISession Session { get; }
		public ReactiveList<IStep> AllSteps { get; } = new ReactiveList<IStep>();
		public IReactiveDerivedList<IStep> Steps { get; } = new ReactiveList<IStep>().CreateDerivedCollection(x => x, x => true);
		public IValidatableReactiveObject ActiveStep => this.Steps[this.TabSelectedIndex];

		public ModelArgumentParser ParsedArguments { get; }


		public ReactiveCommand<object> Next { get; }
		public ReactiveCommand<object> Back { get; }
		public ReactiveCommand<object> Help { get; set; }
		public ReactiveCommand<object> RefreshCurrentStep { get; }
		public ReactiveCommand<object> ShowCurrentStepErrors { get; set; }
		public ReactiveCommand<object> ShowLicenseBlurb { get; set; }
		public ReactiveCommand<object> Exit { get; private set; }

		public ReactiveCommand<IObservable<ClosingResult>> Install { get; }
		public Func<Task<IObservable<ClosingResult>>> InstallUITask { get; set; }

		string nextButtonText;
		public string NextButtonText
		{
			get { return nextButtonText; }
			private set { this.RaiseAndSetIfChanged(ref nextButtonText, value); }
		}

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

		private IList<ValidationFailure> currentValidationFailures = new List<ValidationFailure>();
		public IList<ValidationFailure> CurrentStepValidationFailures
		{
			get { return currentValidationFailures; }
			private set { this.RaiseAndSetIfChanged(ref currentValidationFailures, value); }
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

		private readonly string[] _prerequisiteProperties = new[]
		{
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

		public InstallationModel(
			IWixStateProvider wixStateProvider, 
			IServiceStateProvider serviceStateProvider,
			IPluginStateProvider pluginStateProvider, 
			ISession session,
			string[] args
			)
		{
			this.Session = session;
			
			if (wixStateProvider == null) throw new ArgumentNullException(nameof(wixStateProvider));
			this._wixStateProvider = wixStateProvider;

			var versionConfig = new VersionConfiguration(wixStateProvider);

			this.ServiceModel = new ServiceModel(serviceStateProvider, versionConfig);
			this.PluginsModel = new PluginsModel(pluginStateProvider);

			var observeHost = this.WhenAnyValue(vm => vm.ServiceModel.User, vm => vm.ServiceModel.Password,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;
			this.ClosingModel = new ClosingModel(wixStateProvider.CurrentVersion, isUpgrade, observeHost, observeLog, serviceStateProvider);

			this.AllSteps = new ReactiveList<IStep>
			{
				this.ServiceModel,
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
				vm => vm.PluginsModel.ValidationFailures,
				vm => vm.ServiceModel.ValidationFailures,
				vm => vm.ClosingModel.ValidationFailures,
				vm => vm.TabSelectedIndex,
				(plugins, service, closing, index) =>
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
				vm => vm.PluginsModel.IsValid,
				vm => vm.ServiceModel.IsValid,
				vm => vm.ClosingModel.IsValid,
				(plugins, service, closing) =>
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

		public static InstallationModel Create(
			IWixStateProvider wixState, 
			ISession session, 
			params string[] args
			)
		{
			var serviceState = ServiceStateProvider.FromSession(session);
			var pluginState = PluginStateProvider.Default;
			return new InstallationModel(wixState, serviceState, pluginState, session, args);
		}

		public override void Refresh()
		{
			foreach (var step in this.Steps) step.Refresh();
			this.MsiLogFileLocation = this.Session.Get<string>("MsiLogFileLocation");
		}
	}
}
