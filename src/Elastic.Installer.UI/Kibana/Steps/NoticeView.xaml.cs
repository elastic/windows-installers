using System;
using System.Diagnostics;
using System.Windows;
using Elastic.Installer.Domain.Kibana.Model.Notice;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.UI.Controls;
using ReactiveUI;

namespace Elastic.Installer.UI.Kibana.Steps
{
	public partial class NoticeView : StepControl<NoticeModel, NoticeView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register("ViewModel", typeof(NoticeModel), typeof(NoticeView),
				new PropertyMetadata(null, ViewModelPassed));

		public override NoticeModel ViewModel
		{
			get { return (NoticeModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		public NoticeView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{
			this.OneWayBind(ViewModel, vm => vm.UpgradeText, view => view.UpgradeTextBox.Text);
			this.OneWayBind(ViewModel, vm => vm.UpgradeTextHeader, view => view.UpgradeLabel.Content);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.InstallDir, view => view.InstallationDirectoryLabel.Content);
			this.OneWayBind(ViewModel, vm => vm.LocationsModel.LogsDirectory, view => view.LogsDirectoryLabel.Content);

			var majorMinor = $"{this.ViewModel.CurrentVersion.Major}.{this.ViewModel.CurrentVersion.Minor}";
			this.BindCommand(ViewModel, vm => vm.ReadMoreOnUpgrades, v => v.ReadMoreOnUpgrades, nameof(ReadMoreOnUpgrades.Click));
			this.ViewModel.ReadMoreOnUpgrades.Subscribe(x => Process.Start($"https://www.elastic.co/guide/en/elasticsearch/reference/{majorMinor}/setup-upgrade.html"));

			this.WhenAny(view => view.ViewModel.AlreadyInstalled, v=>v.GetValue())
				.Subscribe(v => {
					this.ReadOnlyPropertiesGrid.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
					this.ReadMoreOnUpgrades.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
					this.ExistingVersionTextBox.Content = string.Format(TextResources.NoticeModel_ExistingVersion, this.ViewModel.ExistingVersion);
					this.ExistingVersionTextBox.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
				});

			this.WhenAny(view => view.ViewModel.InstalledAsService, v=>v.GetValue())
				.Subscribe(v => {
					this.RunAsServiceHeaderLabel.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
					this.RunAsServiceLabel.Visibility = v ? Visibility.Visible : Visibility.Collapsed;
				});

		}
	}
}
