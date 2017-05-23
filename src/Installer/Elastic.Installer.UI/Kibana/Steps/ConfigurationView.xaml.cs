using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Kibana.Configuration;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace Elastic.Installer.UI.Kibana.Steps
{
	/// <summary>
	/// Interaction logic for ConfigurationText.xaml
	/// </summary>
	public partial class ConfigurationView : StepControl<ConfigurationModel, ConfigurationView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ConfigurationModel), typeof(ConfigurationView), new PropertyMetadata(null, ViewModelPassed));

		public override ConfigurationModel ViewModel
		{
			get => (ConfigurationModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		public ConfigurationView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{
			this.Bind(ViewModel, vm => vm.HostName, v => v.HostNameTextBox.Text);
			this.Bind(ViewModel, vm => vm.HttpPort, v => v.HttpPortTextBox.Value, null, new NullableIntToNullableDoubleConverter(), new NullableDoubleToNullableIntConverter());
			this.Bind(ViewModel, vm => vm.ServerName, v => v.ServerNameTextBox.Text);

			this.Bind(ViewModel, vm => vm.BasePath, v => v.BasePathTextBox.Text);
			this.Bind(ViewModel, vm => vm.DefaultRoute, v => v.DefaultRouteTextbox.Text);

			this.Bind(ViewModel, vm => vm.ServerCertificate, v => v.ServerCertificateTextBox.Text);
			this.Bind(ViewModel, vm => vm.ServerKey, v => v.ServerKeyTextBox.Text);
			this.Bind(ViewModel, vm => vm.AllowAnonymousAccess, v => v.AllowAnonymousAccessCheckBox.IsChecked);
		}

		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
		}
	}
}
