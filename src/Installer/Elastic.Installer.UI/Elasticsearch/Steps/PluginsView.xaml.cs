using System.Windows;
using System.Windows.Controls;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class PluginsView : StepControl<PluginsModel, PluginsView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(PluginsModel), typeof(PluginsView), new PropertyMetadata(null, ViewModelPassed));

		public override PluginsModel ViewModel
		{
			get => (PluginsModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		public PluginsView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{
			this.OneWayBind(ViewModel, vm => vm.AvailablePlugins, v => v.PluginsListBox.ItemsSource);

			(Application.Current.MainWindow as MainWindow).ProxyButton.Command = this.ViewModel.SetHttpsProxy;

			this.ViewModel.HttpsProxyUITask = () =>
			{
				var metroWindow = Application.Current.MainWindow as MetroWindow;
				return metroWindow.ShowInputAsync(
					ViewResources.PluginsView_SetHttpsProxy_Title,
					ViewResources.PluginsView_SetHttpsProxy_Message, new MetroDialogSettings
					{
						DefaultText = this.ViewModel.HttpsProxyHostAndPort
					});
			};
		}

		private void OnActualCheckboxClick(object sender, RoutedEventArgs e)
		{
			var source = (e.OriginalSource as CheckBox);
			var plugin = (source?.DataContext as Plugin);
			if (plugin == null || source?.IsChecked == null) return;
			plugin.Selected = source.IsChecked.Value;
		}
	}
}
