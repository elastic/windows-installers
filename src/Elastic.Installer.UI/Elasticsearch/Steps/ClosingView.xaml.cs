using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Elastic.Installer.Domain.Elasticsearch.Model.Closing;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using ReactiveUI;
using Elastic.Installer.Domain.Shared.Model.Closing;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class ClosingView : StepControl<ClosingModel, ClosingView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register("ViewModel", typeof(ClosingModel), typeof(ClosingView), new PropertyMetadata(null, ViewModelPassed));

		public override ClosingModel ViewModel
		{
			get { return (ClosingModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		public ClosingView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{
			this.Bind(ViewModel, vm => vm.OpenDocumentationAfterInstallation, v => v.ReadTheDocsOnCloseCheckBox.IsChecked);
			this.BindCommand(ViewModel, vm => vm.OpenProduct, v => v.OpenElasticsearch, nameof(OpenElasticsearch.Click));
			this.BindCommand(ViewModel, vm => vm.OpenGettingStarted, v => v.OpenGettingStarted, nameof(OpenGettingStarted.Click));
			this.BindCommand(ViewModel, vm => vm.OpenReference, v => v.OpenReference, nameof(OpenReference.Click));
			this.BindCommand(ViewModel, vm => vm.OpenFindYourClient, v => v.OpenFindYourClient, nameof(OpenFindYourClient.Click));
			this.BindCommand(ViewModel, vm => vm.OpenInstallationLog, v => v.OpenInstallationLog, nameof(OpenInstallationLog.Click));
			this.BindCommand(ViewModel, vm => vm.OpenProductLog, v => v.OpenElasticsearchLog, nameof(OpenElasticsearchLog.Click));
			this.BindCommand(ViewModel, vm => vm.OpenIssues, v => v.OpenIssues, nameof(OpenIssues.Click));

			var majorMinor = $"{this.ViewModel.CurrentVersion.Major}.{this.ViewModel.CurrentVersion.Minor}";
			this.OpenReference.Content = string.Format(ViewResources.ClosingView_ReadTheReference, majorMinor);

			var host = "http://localhost:9200";
			this.ViewModel.Host.Subscribe(h => host = h);
			this.ViewModel.OpenProduct.Subscribe(x => Process.Start(host));

			string elasticsearchLog = null;
			this.ViewModel.ProductLog.Subscribe(l => elasticsearchLog = l);
			this.ViewModel.OpenProductLog.Subscribe(x => Process.Start(elasticsearchLog));

			this.ViewModel.OpenReference.Subscribe(x => Process.Start($"https://www.elastic.co/guide/en/elasticsearch/reference/{majorMinor}/index.html"));
			this.ViewModel.OpenGettingStarted.Subscribe(x => Process.Start($"https://www.elastic.co/guide/en/elasticsearch/guide/index.html"));
			this.ViewModel.OpenFindYourClient.Subscribe(x => Process.Start("https://www.elastic.co/guide/en/elasticsearch/client/index.html"));

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

			this.ViewModel.OpenIssues.Subscribe(x => Process.Start("https://github.com/elastic/elasticsearch-windows-installer/issues"));

			this.WhenAnyValue(view => view.ViewModel.Installed)
				.Subscribe(x =>
				{
					this.UpdateGrids(x);
				});
		}

		private static SolidColorBrush FailedBrush = new SolidColorBrush(Color.FromRgb(234, 69, 139));
		private static SolidColorBrush PreemptedBrush = new SolidColorBrush(Color.FromRgb(234, 69, 139));

		private void UpdateGrids(ClosingResult? result)
		{
			this.GridSuccess.Visibility = Visibility.Collapsed;
			this.GridFailed.Visibility = Visibility.Collapsed;
			this.GridCancelled.Visibility = Visibility.Collapsed;
			this.GridPreempted.Visibility = Visibility.Collapsed;

			if (!result.HasValue) result = ClosingResult.Cancelled;

			var installationOrUpgradeLanguage = this.ViewModel.IsUpgrade ? "upgrade" : "installation";
			var installedOrUpgradedLanguage = this.ViewModel.IsUpgrade ? "upgraded" : "installed";

			switch (result.Value)
			{
				case ClosingResult.Success:
					this.GridSuccess.Visibility = Visibility.Visible;
					this.ResultTitleLabel.Content = string.Format(ViewResources.ClosingView_TitleSuccess, "Elasticsearch", installedOrUpgradedLanguage);
					this.ResultParagraphLabel.Visibility = Visibility.Hidden;
					this.OpenElasticsearch.Visibility = 
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
