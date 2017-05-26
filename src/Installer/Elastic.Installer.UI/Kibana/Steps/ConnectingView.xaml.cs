using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Kibana.Connecting;
using Elastic.Installer.UI.Controls;
using FluentValidation.Results;
using ReactiveUI;

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
			get => (ConnectingModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		public ConnectingView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{
			this.Bind(ViewModel, vm => vm.Url, v => v.UrlTextBox.Text);
			this.Bind(ViewModel, vm => vm.IndexName, v => v.IndexTextBox.Text);
			this.Bind(ViewModel, vm => vm.ElasticsearchUsername, v => v.UsernameTextBox.Text);
			this.PasswordTextBox.Events().PasswordChanged
				.Subscribe(a => this.ViewModel.ElasticsearchPassword = this.PasswordTextBox.Password);
		}


		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
		}
	}
}
