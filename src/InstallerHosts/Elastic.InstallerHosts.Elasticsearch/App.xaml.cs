using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base.Closing;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.UI.Elasticsearch;
using ReactiveUI;

namespace Elastic.InstallerHosts.Elasticsearch
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public void Application_Startup(object sender, StartupEventArgs e)
		{
			var wix = new WixStateProvider(Product.Elasticsearch, "5.5.1");
			var model = ElasticsearchInstallationModel.Create(wix, NoopSession.Elasticsearch);

			var window = new MainWindow(model, new ManualResetEvent(false));
			model.InstallUITask = async () =>
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				return Observable.Return(ClosingResult.Success);
			};
			model.AllSteps.Last().IsSelected = true;
			window.Show();

			RxApp.MainThreadScheduler = new DispatcherScheduler(Application.Current.Dispatcher);
			Application.Current.Resources["InstallerTitle"] = wix.CurrentVersion.ToString();
		}
	}
}
