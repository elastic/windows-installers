using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using ReactiveUI;
using static System.Windows.Visibility;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class XPackView : StepControl<XPackModel, XPackView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(XPackModel), typeof(XPackView), new PropertyMetadata(null, ViewModelPassed));

		public override XPackModel ViewModel
		{
			get => (XPackModel) GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		private readonly Brush _defaultBrush;

		public XPackView()
		{
			InitializeComponent();
			this._defaultBrush = this.ElasticPasswordBox.BorderBrush;
		}

		protected override void InitializeBindings()
		{
			// cannot bind to the Password property directly as it does not expose a DP
			this.ElasticPasswordBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.ElasticUserPassword = this.ElasticPasswordBox.Password);
			this.KibanaUserPasswordBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.KibanaUserPassword = this.KibanaUserPasswordBox.Password);
			this.LogstashSystemPasswordBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.LogstashSystemUserPassword = this.LogstashSystemPasswordBox.Password);

			this.Bind(ViewModel, vm => vm.SkipSettingPasswords, v => v.SkipPasswordGenerationCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.XPackSecurityEnabled, v => v.EnableXPackSecurityCheckBox.IsChecked);
			this.BindCommand(ViewModel, vm => vm.OpenLicensesAndSubscriptions, v => v.OpenSubscriptionsLink, nameof(OpenSubscriptionsLink.Click));
			this.BindCommand(ViewModel, vm => vm.OpenManualUserConfiguration, v => v.OpenManualUserConfigurationLink, nameof(OpenManualUserConfigurationLink.Click));

			this.ViewModel.OpenLicensesAndSubscriptions.Subscribe(x => Process.Start(ViewResources.XPackView_OpenLicensesAndSubscriptions));

			var majorMinor = $"{this.ViewModel.CurrentVersion.Major}.{this.ViewModel.CurrentVersion.Minor}";
			this.ViewModel.OpenManualUserConfiguration.Subscribe(x => Process.Start(string.Format(ViewResources.XPackView_ManualUserConfiguration, majorMinor)));

			foreach (var name in Enum.GetNames(typeof(XPackLicenseMode)))
				this.LicenseDropDown.Items.Add(new ComboBoxItem {Content = name});

			this.Bind(ViewModel, vm => vm.XPackLicense, x => x.LicenseDropDown.SelectedIndex
				, null, vmToViewConverterOverride: new LicenseToSelectedIndexConverter()
				, viewToVMConverterOverride: new SelectedIndexToLicenseConverter());

			this.ViewModel.WhenAnyValue(
					vm => vm.XPackLicense,
					vm => vm.SkipSettingPasswords,
					vm => vm.XPackSecurityEnabled,
					vm => vm.CanAutomaticallySetupUsers,
					vm => vm.IsRelevant
				)
				.Subscribe(t =>
				{
					var isTrialLicense = t.Item1 == XPackLicenseMode.Trial;
					var generateUsersLater = t.Item2;
					var securityEnabled = t.Item3;
					var canGenerateUsers = t.Item4;
					var isRelevant = t.Item5;

					this.SecurityGrid.Visibility = isTrialLicense ? Visible : Hidden;
					
					if (!isTrialLicense || !securityEnabled)
						this.UserGrid.Visibility = Collapsed;
					else if (!this.ViewModel.NeedsPasswords)
						this.UserGrid.Visibility = Collapsed;
					else this.UserGrid.Visibility = Visible;
					
					this.ManualSetupGrid.Visibility =
						!isTrialLicense || !securityEnabled
							? Collapsed : (!this.ViewModel.NeedsPasswords ? Visible : Collapsed);

					this.UserLabel.Visibility = isTrialLicense && securityEnabled ? Visible : Collapsed;
					this.SkipPasswordGenerationCheckBox.Visibility = isTrialLicense && securityEnabled ? Visible : Collapsed;
					this.SkipPasswordGenerationCheckBox.IsEnabled = securityEnabled && canGenerateUsers;
					
				});

			this.WhenAnyValue(view => view.ViewModel.ElasticUserPassword,
							  view => view.ViewModel.KibanaUserPassword,
							  view => view.ViewModel.LogstashSystemUserPassword)
				.Subscribe(x =>
				{
					if (x.Item1 == XPackModel.DefaultElasticUserPassword)
						ElasticPasswordBox.Clear();

					if (x.Item2 == XPackModel.DefaultKibanaUserPassword)
						KibanaUserPasswordBox.Clear();

					if (x.Item3 == XPackModel.DefaultLogstashSystemUserPassword)
						LogstashSystemPasswordBox.Clear();
				});
		}

		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
			var b = isValid ? this._defaultBrush : new SolidColorBrush(Color.FromRgb(233, 73, 152));
			this.ElasticPasswordBox.BorderBrush = _defaultBrush;
			this.KibanaUserPasswordBox.BorderBrush = _defaultBrush;
			this.LogstashSystemPasswordBox.BorderBrush = _defaultBrush;
			if (isValid) return;
			foreach (var e in this.ViewModel.ValidationFailures)
			{
				switch (e.PropertyName)
				{
					case nameof(ViewModel.ElasticUserPassword):
						this.ElasticPasswordBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.KibanaUserPassword):
						this.KibanaUserPasswordBox.BorderBrush = b;
						continue;
					case nameof(ViewModel.LogstashSystemUserPassword):
						this.LogstashSystemPasswordBox.BorderBrush = b;
						continue;
				}
			}
		}


		protected class LicenseToSelectedIndexConverter : IBindingTypeConverter
		{
			private static readonly List<XPackLicenseMode> XPackLicenseModes =
				Enum.GetValues(typeof(XPackLicenseMode)).Cast<XPackLicenseMode>().ToList();

			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				result = -1;
				if (!(@from is XPackLicenseMode)) return true;
				var e = (XPackLicenseMode) @from;
				var i = XPackLicenseModes.TakeWhile(v => v != e).Count();
				result = i;
				return true;
			}
		}

		protected class SelectedIndexToLicenseConverter : IBindingTypeConverter
		{
			private static readonly List<XPackLicenseMode> XPackLicenseModes =
				Enum.GetValues(typeof(XPackLicenseMode)).Cast<XPackLicenseMode>().ToList();

			public int GetAffinityForObjects(Type fromType, Type toType) => 1;

			public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
			{
				result = null;
				if (!(@from is int)) return true;
				var i = (int) @from;
				if (i >= 0 && i < XPackLicenseModes.Count)
					result = XPackLicenseModes[i];
				;
				return true;
			}
		}
	}
}