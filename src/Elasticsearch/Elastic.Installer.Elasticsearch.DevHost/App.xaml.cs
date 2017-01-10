using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using Elastic.Installer.Domain.Session;
using Semver;
using System.Reactive.Linq;
using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.UI.Elasticsearch;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Model.Closing;

namespace Elastic.Installer.Elasticsearch.DevHost
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		class DemoWixStateProvider : IWixStateProvider
		{
			public SemVersion CurrentVersion => "5.0.0";

			public SemVersion ExistingVersion => null;
		}

		public void Application_Startup(object sender, StartupEventArgs e)
		{
			var wix = new DemoWixStateProvider();
			var model = ElasticsearchInstallationModel.Create(wix, new NoopSession());

			var window = new MainWindow(model, new ManualResetEvent(false));
			model.InstallUITask = async () =>
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				return Observable.Return(ClosingResult.Failed);
			};
			model.AllSteps.Last().IsSelected = true;
			window.Show();

			RxApp.MainThreadScheduler = new DispatcherScheduler(Application.Current.Dispatcher);

			Application.Current.Resources["InstallerTitle"] = wix.CurrentVersion.ToString();
		}
	}
}
