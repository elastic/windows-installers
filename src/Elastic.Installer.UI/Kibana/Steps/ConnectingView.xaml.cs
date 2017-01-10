using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Elastic.Installer.Domain.Kibana.Model.Connecting;

namespace Elastic.Installer.UI.Kibana.Steps
{
	/// <summary>
	/// Interaction logic for ConfigurationText.xaml
	/// </summary>
	public partial class ConnectingView : StepControl<ConnectingModel, ConnectingView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ConnectingModel), typeof(ConnectingView), new PropertyMetadata(null, ViewModelPassed));

		public override ConnectingModel ViewModel
		{
			get { return (ConnectingModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		public ConnectingView()
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
