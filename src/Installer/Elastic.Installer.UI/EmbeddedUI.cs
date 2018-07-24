using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Deployment.WindowsInstaller;
using ReactiveUI;
using System.Threading.Tasks;
using Elastic.Installer.Domain;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Kibana;
using Elastic.InstallerHosts;

namespace Elastic.Installer.UI
{
	public interface IEmbeddedWindow
	{
		MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton);
		Task EnableExit();
		Dispatcher Dispatcher { get; }
	}

	public class EmbeddedUI : IEmbeddedUI
	{
		private Thread _appThread;
		private Application _app;
		private IEmbeddedWindow _mainWindow;
		private ManualResetEvent _installStartEvent;
		private ManualResetEvent _installExitEvent;
		private Session _session;

		public bool Initialize(Session session, string resourcePath, ref InstallUIOptions internalUILevel)
		{
			if (session == null)
				throw new ArgumentNullException(nameof(session));

			if ((internalUILevel & InstallUIOptions.Full) != InstallUIOptions.Full)
				return false;

			if (string.Equals(session["REMOVE"], "All", StringComparison.OrdinalIgnoreCase))
				return false;

			this._session = session;
			this._installStartEvent = new ManualResetEvent(false);
			this._installExitEvent = new ManualResetEvent(false);
			this._appThread = new Thread(this.Run);
			this._appThread.SetApartmentState(ApartmentState.STA);
			this._appThread.Start();

			int waitResult = WaitHandle.WaitAny(new WaitHandle[] { this._installStartEvent, this._installExitEvent });
			if (waitResult == 1)
				throw new InstallCanceledException();
			internalUILevel = InstallUIOptions.NoChange | InstallUIOptions.SourceResolutionOnly;
			return true;
		}

		public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord,
			MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
		{
			// skip showing any FilesInUse dialog, which is shown because the Windows service
			// is running and will be stopped/removed/started
			if (messageType == InstallMessage.RMFilesInUse || messageType == InstallMessage.FilesInUse)
				return MessageResult.OK;

			object result = this._mainWindow.Dispatcher.Invoke(
				DispatcherPriority.Send,
				new Func<MessageResult>(() => this._mainWindow.ProcessMessage(messageType, messageRecord, buttons, icon, defaultButton)));

			return (MessageResult)result;
		}

		public void Shutdown()
		{
			this._mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
				new Action(async delegate
				{
					await this._mainWindow.EnableExit();
				}));
			this._appThread.Join();
		}

		private void Run()
		{
			this._app = new Application();
			RxApp.MainThreadScheduler = new DispatcherScheduler(_app.Dispatcher);

			if (!this._session.TryGetValue("CurrentVersion", out var version))
				throw new Exception("CurrentVersion not found in session state.");

			if (!this._session.TryGetValue("ElasticProduct", out var product))
				throw new Exception("ElasticProduct not found in session state.");

			var wixStateProvider = new WixStateProvider(GetProduct(product), version, installationInProgress: false);
			this._mainWindow = GetMainWindow(product, wixStateProvider, new SessionWrapper(_session));

			Application.ResourceAssembly = _mainWindow.GetType().Assembly;
			this._app.Run(this._mainWindow as Window);
			this._installExitEvent.Set();
		}

		private IEmbeddedWindow GetMainWindow(string product, IWixStateProvider wixState, ISession session)
		{
			switch (product)
			{
				case "Elasticsearch":
					{
						var model = ElasticsearchInstallationModel.Create(wixState, session);
						var window = new Elasticsearch.MainWindow(model, this._installStartEvent);
						return window;
					}
				case "Kibana":
					{
						var model = KibanaInstallationModel.Create(wixState, session);
						var window = new Kibana.MainWindow(model, this._installStartEvent);
						return window;
					}
				default:
					throw new Exception($"Unknown product name {product}");
			}
		}

		private static Product GetProduct(string product)
		{
			switch(product)
			{
				case "Elasticsearch": return Product.Elasticsearch;
				case "Kibana": return Product.Kibana;
				default: throw new ArgumentException($"Unknown product name {product}");
			}
		}
	}
}
