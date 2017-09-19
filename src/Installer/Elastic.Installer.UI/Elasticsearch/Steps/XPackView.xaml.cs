using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.Installer.UI.Controls;
using ReactiveUI;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class XPackView : StepControl<XPackModel, XPackView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(XPackModel), typeof(XPackView), new PropertyMetadata(null, ViewModelPassed));

		public override XPackModel ViewModel
		{
			get => (XPackModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		public XPackView()
		{
			InitializeComponent();
		}

		protected override void InitializeBindings()
		{
			//this.OneWayBind(ViewModel, vm => vm.AvailablePlugins, v => v.PluginsListBox.ItemsSource);
		}
	}
}
