using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Deployment.WindowsInstaller;
using ReactiveUI;
using Elastic.Installer.Domain.Session;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.UI.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Elasticsearch.Model;

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

			_session = session;

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
			object result = this._mainWindow.Dispatcher.Invoke(
				DispatcherPriority.Send,
				new Func<MessageResult>(() => this._mainWindow.ProcessMessage(messageType, messageRecord, buttons, icon, defaultButton)));

			return (MessageResult)result;
		}

		public void Shutdown()
		{
			this._mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
				new Action(async delegate ()
				{
					await this._mainWindow.EnableExit();
				}));
			this._appThread.Join();
		}

		private void Run()
		{
			this._app = new Application();
			RxApp.MainThreadScheduler = new DispatcherScheduler(_app.Dispatcher);

			string version;
			if (!this._session.TryGetValue("CurrentVersion", out version))
				throw new Exception("CurrentVersion not found in session state.");

			string product;
			if (!this._session.TryGetValue("ElasticProduct", out product))
				throw new Exception("ProductName not found in session state.");

			this._mainWindow = GetMainWindow(product, new WixStateProvider(GetProduct(product), version), new SessionWrapper(_session));

			Application.ResourceAssembly = _mainWindow.GetType().Assembly;
			this._app.Run(this._mainWindow as Window);
			this._installExitEvent.Set();
		}

		private IEmbeddedWindow GetMainWindow(string product, IWixStateProvider wixState, ISession session)
		{
			switch (product)
			{
				case "elasticsearch":
					{
						var model = ElasticsearchInstallationModel.Create(wixState, session);
						var window = new Elasticsearch.MainWindow(model, this._installStartEvent);
						return window;
					}
				case "kibana":
					{
						var model = KibanaInstallationModel.Create(wixState, session);
						var window = new Kibana.MainWindow(model, this._installStartEvent);
						return window;
					}
				default:
					throw new Exception($"Unknown product name {product}");
			}
		}

		private Product GetProduct(string name)
		{
			switch(name.ToLowerInvariant())
			{
				case "elasticsearch": return Product.Elasticsearch;
				case "kibana": return Product.Kibana;
				default: throw new ArgumentException($"Unknown product name {name}");
			}
		}
	}
}
