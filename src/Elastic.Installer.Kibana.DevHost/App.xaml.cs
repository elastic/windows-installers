using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Model.Closing;
using Elastic.Installer.UI;
using Elastic.Installer.UI.Kibana;
using ReactiveUI;
using Semver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Elastic.Installer.Kibana.DevHost
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
			var model = KibanaInstallationModel.Create(wix, new NoopSession());

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
