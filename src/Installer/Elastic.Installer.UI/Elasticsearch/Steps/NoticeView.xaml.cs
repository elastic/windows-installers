using System;
using System.Diagnostics;
using System.Windows;
using Elastic.Installer.Domain.Model.Elasticsearch.Notice;
using Elastic.Installer.Domain.Properties;
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
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.InstallDir, view => view.InstallationDirectoryLabel.Content);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.DataDirectory, view => view.DataDirectoryLabel.Content);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.ConfigDirectory, view => view.ConfigDirectoryLabel.Content);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.LogsDirectory, view => view.LogsDirectoryLabel.Content);

			var majorMinor = $"{this.ViewModel.CurrentVersion.Major}.{this.ViewModel.CurrentVersion.Minor}";
			this.BindCommand(ViewModel, vm => vm.ReadMoreOnUpgrades, v => v.ReadMoreOnUpgrades, nameof(ReadMoreOnUpgrades.Click));
			this.ViewModel.ReadMoreOnUpgrades.Subscribe(x => Process.Start(string.Format(ViewResources.NoticeView_Elasticsearch_ReadMoreOnUpgrades, majorMinor)));

			this.WhenAny(view => view.ViewModel.ExistingVersionInstalled, v=>v.GetValue())
				.Subscribe(v => {
					this.ReadOnlyPropertiesGrid.Visibility = v ? Visible : Collapsed;
					this.ReadMoreOnUpgrades.Visibility = v ? Visible : Collapsed;
//					this.ExistingVersionTextBox.Content = string.Format(TextResources.NoticeModel_ExistingVersion, this.ViewModel.ExistingVersion);
//					this.ExistingVersionTextBox.Visibility = v ? Visible : Collapsed;
				});

			//Using a separate observable in the case we add more parameters to the control grid on the notice model
			this.WhenAny(view => view.ViewModel.InstalledAsService, v => v.GetValue())
				.Subscribe(v =>
				{
					this.ControlGrid.Visibility = v ? Visible : Collapsed;
				});

			this.WhenAny(view => view.ViewModel.InstalledAsService, v=>v.GetValue())
				.Subscribe(v => {
					this.RunAsServiceHeaderLabel.Visibility = v ? Visible : Collapsed;
					this.RunAsServiceLabel.Visibility = v ? Visible : Collapsed;
					this.StartServiceAfterInstallCheckBox.Visibility = v ? Visible : Collapsed;
				});
			
			this.Bind(ViewModel, vm => vm.ServiceModel.StartAfterInstall, v => v.StartServiceAfterInstallCheckBox.IsChecked);
		}
	}
}
