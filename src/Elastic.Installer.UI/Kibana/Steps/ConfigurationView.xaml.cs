using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Elastic.Installer.Domain.Kibana.Model.Configuration;
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
			get { return (ConfigurationModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		public ConfigurationView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{

		}


		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{

		}
	}
}
