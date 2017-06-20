using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.UI.Controls;
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
