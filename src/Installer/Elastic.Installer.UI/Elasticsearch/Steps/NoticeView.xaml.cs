using System;
using System.Diagnostics;
using System.Windows;
using Elastic.Installer.Domain.Model.Elasticsearch.Notice;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using ReactiveUI;
using static System.Windows.Visibility;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class NoticeView : StepControl<NoticeModel, NoticeView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(NoticeModel), typeof(NoticeView), new PropertyMetadata(null, ViewModelPassed));

		public override NoticeModel ViewModel
		{
			get => (NoticeModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		public NoticeView() => InitializeComponent();
		
		protected override void InitializeBindings()
		{
			this.OneWayBind(ViewModel, vm => vm.UpgradeText, view => view.UpgradeTextBox.Text);
			this.OneWayBind(ViewModel, vm => vm.UpgradeTextHeader, view => view.UpgradeLabel.Content);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.InstallDir, view => view.InstallationDirectoryTextBlock.Text);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.DataDirectory, view => view.DataDirectoryTextBlock.Text);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.ConfigDirectory, view => view.ConfigDirectoryTextBlock.Text);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.LogsDirectory, view => view.LogsDirectoryTextBlock.Text);
			this.Bind(ViewModel, vm => vm.ServiceModel.StartAfterInstall, v => v.StartServiceAfterInstallCheckBox.IsChecked);

			var majorMinor = $"{this.ViewModel.CurrentVersion.Major}.{this.ViewModel.CurrentVersion.Minor}";
			this.BindCommand(ViewModel, vm => vm.ReadMoreOnUpgrades, v => v.ReadMoreOnUpgrades, nameof(ReadMoreOnUpgrades.Click));
			this.ViewModel.ReadMoreOnUpgrades.Subscribe(x => Process.Start(string.Format(ViewResources.NoticeView_Elasticsearch_ReadMoreOnUpgrades, majorMinor)));
			
			this.BindCommand(ViewModel, vm => vm.ReadMoreOnXPackOpening, v => v.ReadMoreOnXPackOpening, nameof(ReadMoreOnXPackOpening.Click));
			this.ViewModel.ReadMoreOnXPackOpening.Subscribe(x => Process.Start(ViewResources.NoticeView_Elasticsearch_ReadMoreOnXPackOpening));

			this.WhenAnyValue(v => v.ViewModel.ServiceModel.PreviouslyInstalledAsAService)
				.Subscribe(b =>
				{
					var visibility = b ? Visible : Collapsed;
					RunAsServiceHeaderLabel.Visibility = visibility;
					RunAsServiceLabel.Visibility = visibility;
					StartServiceAfterInstallCheckBox.Visibility = visibility;
				});

			this.WhenAnyValue(v => v.ViewModel.ShowOpeningXPackBanner)
				.Subscribe(b =>
				{
					this.XPackLogo.Visibility = b ? Visible : Collapsed;
					this.XPackStackPanel.Visibility = b ? Visible : Collapsed;				
				});

			this.WhenAnyValue(view => view.ViewModel.ExistingVersionInstalled, view => view.ViewModel.ShowUpgradeDocumentationLink)
				.Subscribe(tuple =>
				{
					bool installed = tuple.Item1, showUpgrade = tuple.Item2;
					this.ReadOnlyPropertiesGrid.Visibility = installed ? Visible : Collapsed;
					this.ReadMoreOnUpgrades.Visibility = installed && showUpgrade ? Visible : Collapsed;
				});
		}
	}
}
