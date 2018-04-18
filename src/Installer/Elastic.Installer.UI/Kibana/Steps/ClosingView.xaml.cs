using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Base.Closing;
using Elastic.Installer.Domain.Model.Kibana.Closing;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using ReactiveUI;

namespace Elastic.Installer.UI.Kibana.Steps
{
	public partial class ClosingView : StepControl<ClosingModel, ClosingView>
	{
		private static readonly SolidColorBrush FailedBrush = new SolidColorBrush(Color.FromRgb(234, 69, 139));

		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ClosingModel), typeof(ClosingView), new PropertyMetadata(null, ViewModelPassed));

		public override ClosingModel ViewModel
		{
			get => (ClosingModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		public ClosingView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{
			this.Bind(ViewModel, vm => vm.OpenDocumentationAfterInstallation, v => v.ReadTheDocsOnCloseCheckBox.IsChecked);
			this.BindCommand(ViewModel, vm => vm.OpenProduct, v => v.OpenKibana, nameof(OpenKibana.Click));
			this.BindCommand(ViewModel, vm => vm.OpenGettingStarted, v => v.OpenGettingStarted, nameof(OpenGettingStarted.Click));
			this.BindCommand(ViewModel, vm => vm.OpenReference, v => v.OpenReference, nameof(OpenReference.Click));
			this.BindCommand(ViewModel, vm => vm.OpenInstallationLog, v => v.OpenInstallationLog, nameof(OpenInstallationLog.Click));
			this.BindCommand(ViewModel, vm => vm.OpenIssues, v => v.OpenIssues, nameof(OpenIssues.Click));

			var majorMinor = $"{this.ViewModel.CurrentVersion.Major}.{this.ViewModel.CurrentVersion.Minor}";
			this.OpenReference.Content = string.Format(ViewResources.ClosingView_ReadTheReference, majorMinor);

			var host = "http://localhost:5601";
			this.ViewModel.Host.Subscribe(h => host = h);
			this.ViewModel.OpenProduct.Subscribe(x => Process.Start(host));

			this.ViewModel.OpenReference.Subscribe(x => Process.Start(string.Format(ViewResources.ClosingView_Kibana_OpenReference, majorMinor)));
			this.ViewModel.OpenGettingStarted.Subscribe(x => Process.Start(string.Format(ViewResources.ClosingView_Kibana_OpenGettingStarted, majorMinor)));

				//TODO this should listen to basic (no security) vs trial (security)
				this.OpenKibana.Content = false
					? ViewResources.ClosingView_KibanaRunningAtHeaderWithCredentials
					: ViewResources.ClosingView_KibanaRunningAtHeader;

			string wixLog = null;
			this.ViewModel.WixLogFile.Subscribe(l =>
			{
				wixLog = l;
				this.OpenInstallationLog.Visibility = string.IsNullOrWhiteSpace(l) ? Visibility.Collapsed : Visibility.Visible;
			});
			this.ViewModel.OpenInstallationLog.Subscribe(x =>
			{
				if (wixLog != null) Process.Start(wixLog);
			});

			this.ViewModel.OpenIssues.Subscribe(x => Process.Start(ViewResources.ClosingView_GithubIssues));

			this.WhenAnyValue(view => view.ViewModel.Installed)
				.Subscribe(this.UpdateGrids);
		}

		private void UpdateGrids(ClosingResult? result)
		{
			this.GridSuccess.Visibility = Visibility.Collapsed;
			this.GridFailed.Visibility = Visibility.Collapsed;
			this.GridCancelled.Visibility = Visibility.Collapsed;
			this.GridPreempted.Visibility = Visibility.Collapsed;

			if (!result.HasValue) result = ClosingResult.Cancelled;

			var installationOrUpgradeLanguage = this.ViewModel.IsUpgrade
				? ViewResources.ClosingView_UpgradeText
				: ViewResources.ClosingView_InstallationText;

			var installedOrUpgradedLanguage = this.ViewModel.IsUpgrade
				? ViewResources.ClosingView_UpgradedText
				: ViewResources.ClosingView_InstalledText;

			switch (result.Value)
			{
				case ClosingResult.Success:
					this.GridSuccess.Visibility = Visibility.Visible;
					this.ResultTitleLabel.Content = string.Format(ViewResources.ClosingView_TitleSuccess, "Kibana", installedOrUpgradedLanguage);
					this.ResultParagraphLabel.Visibility = Visibility.Hidden;
					this.OpenKibana.Visibility = 
						this.ViewModel.ServiceStateProvider.Running ? Visibility.Visible : Visibility.Collapsed;
					break;
				case ClosingResult.Failed:
					this.GridFailed.Visibility = Visibility.Visible;
					this.ResultTitleLabel.Content = string.Format(ViewResources.ClosingView_TitleFailed, installationOrUpgradeLanguage);
					this.SubTitleFailed.Foreground = FailedBrush;
					this.ResultTitleLabel.Foreground = FailedBrush;
					this.ResultParagraphLabel.Text = ViewResources.ClosingView_ParagraphFailed;
					break;
				case ClosingResult.Cancelled:
					this.ResultTitleLabel.Content = string.Format(ViewResources.ClosingView_TitleCancelled, installationOrUpgradeLanguage);
					this.ResultParagraphLabel.Text = ViewResources.ClosingView_ParagraphCancelled;
					this.GridCancelled.Visibility = Visibility.Visible;
					break;
				case ClosingResult.Preempted:
					this.ResultTitleLabel.Content = string.Format(ViewResources.ClosingView_TitlePreempted, installationOrUpgradeLanguage);
					this.ResultParagraphLabel.Text = ViewResources.ClosingView_ParagraphPreempted;
					this.GridPreempted.Visibility = Visibility.Visible;
					break;
			}
		}

		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
		}
	}
}
