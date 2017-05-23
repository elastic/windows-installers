using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Elastic.Installer.UI.Controls;
using FluentValidation.Results;
using ReactiveUI;
using Elastic.Installer.Domain.Shared.Model.Service;

namespace Elastic.Installer.UI.Shared.Steps
{
	public partial class ServiceView : StepControl<ServiceModel, ServiceView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ServiceModel), typeof(ServiceView), new PropertyMetadata(null, ViewModelPassed));

		public override ServiceModel ViewModel
		{
			get => (ServiceModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
		
		private readonly Brush _defaultBrush;
		public ServiceView()
		{
			InitializeComponent();
			this._defaultBrush = this.UserTextBox.BorderBrush;
		}

		protected override void InitializeBindings()
		{
			this.Bind(ViewModel, vm => vm.InstallAsService, v => v.InstallAsServiceRadioButton.IsChecked);
			this.Bind(ViewModel, vm => vm.StartAfterInstall, v => v.StartAfterInstallCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.StartWhenWindowsStarts, v => v.StartWhenWindowsStartsCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.UseLocalSystem, v => v.LocalSystemRadioButton.IsChecked);
			this.Bind(ViewModel, vm => vm.UseExistingUser, v => v.ExistingUserRadioButton.IsChecked);
			this.Bind(ViewModel, vm => vm.UseNetworkService, v => v.NetworkServiceRadioButton.IsChecked);
			this.Bind(ViewModel, vm => vm.User, v => v.UserTextBox.Text);
			this.BindCommand(ViewModel, vm => vm.ValidateCredentials, v => v.ValidateCredentials, nameof(ValidateCredentials.Click));


			this.WhenAnyValue(view => view.ViewModel.ValidatingCredentials)
				.Subscribe(x =>
				{
					this.ValidationProgress.Visibility = x ? Visibility.Visible : Visibility.Collapsed;
					this.ValidateCredentials.Visibility = !x ? Visibility.Visible : Visibility.Collapsed;
				});

			this.WhenAnyValue(view => view.ViewModel.InstallAsService)
				.Subscribe(x =>
				{
					this.ServiceGrid.Visibility = x ? Visibility.Visible : Visibility.Collapsed;
					this.StartAfterInstallCheckBox.IsEnabled = x;
					this.StartWhenWindowsStartsCheckBox.IsEnabled = x;
					this.NetworkServiceRadioButton.IsEnabled = x;
					this.LocalSystemRadioButton.IsEnabled = x;
					this.ExistingUserRadioButton.IsEnabled = x;
					this.ManualRadioButton.IsChecked = !x;
				});

			this.WhenAny(view => view.ViewModel.UseExistingUser, view=>view.ViewModel.InstallAsService, (existing, asService) => existing.GetValue() && asService.GetValue())
				.Subscribe(x =>
				{
					this.UserGrid.Visibility = x ? Visibility.Visible : Visibility.Collapsed;
					this.UserLabel.IsEnabled = x;
					this.UserTextBox.IsEnabled = x;
					this.PasswordLabel.IsEnabled = x;
					this.PasswordTextBox.IsEnabled = x;
					this.ValidateCredentials.IsEnabled = x;
				});

			// cannot bind to the Password property directly as it does not expose a DP
			this.PasswordTextBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.Password = this.PasswordTextBox.Password);
		}

		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
			var b = isValid ? this._defaultBrush : new SolidColorBrush(Color.FromRgb(233,73,152));
			this.UserTextBox.BorderBrush = _defaultBrush;
			this.PasswordTextBox.BorderBrush = _defaultBrush;
			this.ValidateCredentials.BorderBrush = _defaultBrush;
			if (isValid) return;
			foreach (var e in this.ViewModel.ValidationFailures)
			{
				switch (e.PropertyName)
				{
					case "User":
						this.UserTextBox.BorderBrush = b;
						continue;
					case "Password":
						this.PasswordTextBox.BorderBrush = b;
						continue;
					case "ManualValidationPassed":
						this.ValidateCredentials.BorderBrush = b;
						continue;
				}
			}
		}
	}
}
