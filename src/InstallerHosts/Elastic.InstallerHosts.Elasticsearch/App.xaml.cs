﻿using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Elastic.Installer.Domain.Model.Base.Closing;
using Elastic.Installer.Domain.Tests.Elasticsearch.Models;
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
			var state = InstallationModelTester.ValidPreflightChecks(s => s
				.Wix(current: "6.3.0", upgradeFrom: "6.2.3")
				//.Wix(current: "6.3.0")
				.Elasticsearch(es=>es
					.EsHomeMachineVariable(@"C:\elasticsearch\6.2.3")
					.EsConfigMachineVariable(@"C:\elasticsearch\6.2.3\config")
				)
				.ServicePreviouslyInstalled()
			);
			var model = state.InstallationModel;

			var window = new MainWindow(model, new ManualResetEvent(false));
			model.InstallUITask = async () =>
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				return Observable.Return(ClosingResult.Success);
			};
			model.AllSteps.Last().IsSelected = true;
			window.Show();

			RxApp.MainThreadScheduler = new DispatcherScheduler(Application.Current.Dispatcher);
			Application.Current.Resources["InstallerTitle"] = model.ClosingModel.CurrentVersion.ToString();
		}
	}
}
