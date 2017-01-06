using System;
using System.Reactive.Concurrency;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Deployment.WindowsInstaller;
using ReactiveUI;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.UI.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.UI.Elasticsearch;

namespace Elastic.Installer.UI
{
	public class ElasticsearchEmbeddedUI : IEmbeddedUI
	{
		private Thread _appThread;
		private Application _app;
		private MainWindow _mainWindow;
		private ManualResetEvent _installStartEvent;
		private ManualResetEvent _installExitEvent;

		private Session _session;
		/// <summary>
		/// Initializes the embedded UI.
		/// </summary>
		/// <param name="session">Handle to the installer which can be used to get and set properties.
		/// The handle is only valid for the duration of this method call.</param>
		/// <param name="resourcePath">Path to the directory that contains all the files from the MsiEmbeddedUI table.</param>
		/// <param name="internalUILevel">On entry, contains the current UI level for the installation. After this
		/// method returns, the installer resets the UI level to the returned value of this parameter.</param>
		/// <returns>True if the embedded UI was successfully initialized; false if the installation
		/// should continue without the embedded UI.</returns>
		/// <exception cref="InstallCanceledException">The installation was canceled by the user.</exception>
		/// <exception cref="InstallerException">The embedded UI failed to initialize and
		/// causes the installation to fail.</exception>
		public bool Initialize(Session session, string resourcePath, ref InstallUIOptions internalUILevel)
		{
			if (session != null)
			{
				if ((internalUILevel & InstallUIOptions.Full) != InstallUIOptions.Full)
				{
					// Don't show custom UI when the UI level is set to basic.
					return false;

					// An embedded UI could display an alternate dialog sequence for reduced or
					// basic modes, but it's not implemented here. We'll just fall back to the
					// built-in MSI basic UI.
				}
				if (string.Equals(session["REMOVE"], "All", StringComparison.OrdinalIgnoreCase))
				{
					// Don't show custom UI when uninstalling.
					return false;

					// An embedded UI could display an uninstall wizard, it's just not implemented here.
				}
			}

			_session = session;

			// Start the UI on a separate thread.
			this._installStartEvent = new ManualResetEvent(false);
			this._installExitEvent = new ManualResetEvent(false);
			this._appThread = new Thread(this.Run);
			this._appThread.SetApartmentState(ApartmentState.STA);
			this._appThread.Start();

			// Wait for the setup wizard to either kickoff the install or prematurely exit.
			int waitResult = WaitHandle.WaitAny(new WaitHandle[] { this._installStartEvent, this._installExitEvent });
			if (waitResult == 1)
			{
				// The setup wizard set the exit event instead of the start event. Cancel the installation.
				throw new InstallCanceledException();
			}
			else
			{
				// Start the installation with a silenced internal UI.
				// This "embedded external UI" will handle message types except for source resolution.
				internalUILevel = InstallUIOptions.NoChange | InstallUIOptions.SourceResolutionOnly;
				return true;
			}
		}

		/// <summary>
		/// Processes information and progress messages sent to the user interface.
		/// </summary>
		/// <param name="messageType">Message type.</param>
		/// <param name="messageRecord">Record that contains message data.</param>
		/// <param name="buttons">Message box buttons.</param>
		/// <param name="icon">Message box icon.</param>
		/// <param name="defaultButton">Message box default button.</param>
		/// <returns>Result of processing the message.</returns>
		public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord,
			MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
		{
			// Synchronously send the message to the setup wizard window on its thread.
			object result = this._mainWindow.Dispatcher.Invoke(
				DispatcherPriority.Send,
				new Func<MessageResult>(() => this._mainWindow.ProcessMessage(messageType, messageRecord, buttons, icon, defaultButton)));

			return (MessageResult)result;
		}

		/// <summary>
		/// Shuts down the embedded UI at the end of the installation.
		/// </summary>
		/// <remarks>
		/// If the installation was canceled during initialization, this method will not be called.
		/// If the installation was canceled or failed at any later point, this method will be called at the end.
		/// </remarks>
		public void Shutdown()
		{
			// Wait for the user to exit the setup wizard.
			this._mainWindow.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
				new Action(async delegate ()
				{
					await this._mainWindow.EnableExit();
				}));
			this._appThread.Join();
		}

		/// <summary>
		/// Creates the setup wizard and runs the application thread.
		/// </summary>
		private void Run()
		{
			this._app = new Application();
			RxApp.MainThreadScheduler = new DispatcherScheduler(_app.Dispatcher);
			Application.ResourceAssembly = typeof(MainWindow).Assembly;

			string version;
			this._session.TryGetValue("CurrentVersion", out version);

			var model = InstallationModel.Create(new WixStateProvider(version), new SessionWrapper(_session));
			this._mainWindow = new MainWindow(model, this._installStartEvent);
			this._app.Run(this._mainWindow);
			this._installExitEvent.Set();
		}
	}
}
