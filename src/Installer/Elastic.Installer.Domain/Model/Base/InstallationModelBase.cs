using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base.Closing;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Properties;
using FluentValidation;
using FluentValidation.Results;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Base
{
	public abstract class InstallationModelBase<TModel, TModelValidator> : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModel : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModelValidator : AbstractValidator<TModel>, new()
	{
		protected readonly IWixStateProvider _wixStateProvider;

		protected virtual string[] PrerequisiteProperties => new[]
		{
			nameof(SameVersionAlreadyInstalled),
			nameof(HigherVersionAlreadyInstalled)
		};

		public ISession Session { get; }

		public ReactiveList<IStep> AllSteps { get; }
		public IReactiveDerivedList<IStep> Steps { get; } 

		public IStep ActiveStep => this.Steps[this.TabSelectedIndex];

		public ReactiveCommand<object> Next { get; }
		public ReactiveCommand<object> Back { get; }
		public ReactiveCommand<object> Proxy { get; }
		public ReactiveCommand<object> Help { get; set; }
		public ReactiveCommand<object> RefreshCurrentStep { get; }
		public ReactiveCommand<object> ShowCurrentStepErrors { get; set; }
		public ReactiveCommand<object> ShowLicenseBlurb { get; set; }
		public ReactiveCommand<object> Exit { get; }
		public ReactiveCommand<IObservable<ClosingResult>> Install { get; protected set; }
		public Func<Task<IObservable<ClosingResult>>> InstallUITask { get; set; }
		public ModelArgumentParser ParsedArguments { get; protected set; }

		protected InstallationModelBase(
			IWixStateProvider wixStateProvider,
			ISession session,
			string[] args
		)
		{
			this.Session = session;

			this.AllSteps = new ReactiveList<IStep> {ChangeTrackingEnabled = true};
			this.Steps = this.AllSteps.CreateDerivedCollection(x => x, x => x.IsRelevant);
			this.Steps.Changed.Subscribe(e => this.Steps.RaisePropertyChanged());

			this._wixStateProvider = wixStateProvider ?? throw new ArgumentNullException(nameof(wixStateProvider));

			this.NextButtonText = TextResources.SetupView_NextText;

			var canMoveForwards = this.WhenAny(vm => vm.TabSelectedIndex, vm => vm.TabSelectionMax,
				(i, max) => i.GetValue() < max.GetValue());

			this.Next = ReactiveCommand.Create(canMoveForwards);
			this.Next.Subscribe(i => { this.TabSelectedIndex = Math.Min(this.Steps.Count - 1, this.TabSelectedIndex + 1); });

			var canMoveBackwards = this.WhenAny(vm => vm.TabSelectedIndex, i => i.GetValue() > 0);
			this.Back = ReactiveCommand.Create(canMoveBackwards);
			this.Back.Subscribe(i => { this.TabSelectedIndex = Math.Max(0, this.TabSelectedIndex - 1); });

			this.Help = ReactiveCommand.Create();
			this.ShowLicenseBlurb = ReactiveCommand.Create();
			this.ShowCurrentStepErrors = ReactiveCommand.Create();
			this.RefreshCurrentStep = ReactiveCommand.Create();
			this.RefreshCurrentStep.Subscribe(x => { this.Steps[this.TabSelectedIndex].Refresh(); });
			this.Exit = ReactiveCommand.Create();
			this.Proxy = ReactiveCommand.Create();

			this.Steps.Changed.Subscribe((e => UpdateNextButtonText(this.TabSelectedIndex)));
			this.WhenAny(vm => vm.TabSelectedIndex, v => v.GetValue())
				.Subscribe(i =>
				{
					this.UpdateNextButtonText(i);
					var c = this.Steps.Count;
					ProxyButtonVisible = c > 0 && ActiveStep is PluginsModel;
				});

			this.WhenAnyValue(view => view.ValidationFailures)
				.Subscribe(failures =>
				{
					this.PrerequisiteFailures = (failures ?? Enumerable.Empty<ValidationFailure>())
						.Where(v => PrerequisiteProperties.Contains(v.PropertyName))
						.ToList();
				});
		}

		private void UpdateNextButtonText(int i)
		{
			var c = this.Steps.Count;
			if (i == (c - 1)) this.NextButtonText = TextResources.SetupView_ExitText;
			else if (i == (c - 2)) this.NextButtonText = TextResources.SetupView_InstallText;
			else this.NextButtonText = TextResources.SetupView_NextText;
		}

		string nextButtonText;
		public string NextButtonText
		{
			get => nextButtonText;
			private set => this.RaiseAndSetIfChanged(ref nextButtonText, value);
		}

		string msiLogFileLocation;
		public string MsiLogFileLocation
		{
			get => msiLogFileLocation;
			set => this.RaiseAndSetIfChanged(ref msiLogFileLocation, value);
		}

		int tabSelectionMax;
		public int TabSelectionMax
		{
			get => tabSelectionMax;
			protected set => this.RaiseAndSetIfChanged(ref tabSelectionMax, value);
		}

		int tabSelectedIndex;
		public int TabSelectedIndex
		{
			get => tabSelectedIndex;
			set => this.RaiseAndSetIfChanged(ref tabSelectedIndex, value);
		}

		private IList<ValidationFailure> currentValidationFailures = new List<ValidationFailure>();

		public IList<ValidationFailure> CurrentStepValidationFailures
		{
			get => currentValidationFailures;
			protected set => this.RaiseAndSetIfChanged(ref currentValidationFailures, value);
		}

		bool sameVersionAlreadyInstalled;
		public bool SameVersionAlreadyInstalled
		{
			get => sameVersionAlreadyInstalled;
			set => this.RaiseAndSetIfChanged(ref sameVersionAlreadyInstalled, value);
		}

		bool higherVersionAlreadyInstalled;
		public bool HigherVersionAlreadyInstalled
		{
			get => higherVersionAlreadyInstalled;
			set => this.RaiseAndSetIfChanged(ref higherVersionAlreadyInstalled, value);
		}

		bool proxyButtonVisible;
		public bool ProxyButtonVisible
		{
			get => proxyButtonVisible;
			set => this.RaiseAndSetIfChanged(ref proxyButtonVisible, value);
		}

		public string ToMsiParamsString() => this.ParsedArguments.ToMsiParamsString();

		public IEnumerable<ModelArgument> ToMsiParams() => this.ParsedArguments.ToMsiParams();
	}
}