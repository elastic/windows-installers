using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.ServiceProcess;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Configuration.Plugin;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Base.Plugins;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Kibana.Closing;
using Elastic.Installer.Domain.Model.Kibana.Configuration;
using Elastic.Installer.Domain.Model.Kibana.Connecting;
using Elastic.Installer.Domain.Model.Kibana.Locations;
using Elastic.Installer.Domain.Model.Kibana.Notice;
using Elastic.Installer.Domain.Model.Kibana.Plugins;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Kibana
{
	public class KibanaInstallationModel
		: InstallationModelBase<KibanaInstallationModel, KibanaInstallationModelValidator>
	{
		public IKibanaEnvironmentStateProvider KibanaEnvironmentState { get; }

		public NoticeModel NoticeModel { get; }
		public LocationsModel LocationsModel { get; }
		public ServiceModel ServiceModel { get; }
		public ConfigurationModel ConfigurationModel { get; }
		public ConnectingModel ConnectingModel { get; }
		public PluginsModel PluginsModel { get; }
		public ClosingModel ClosingModel { get; }

		public KibanaInstallationModel(
			IWixStateProvider wixStateProvider,
			IServiceStateProvider serviceStateProvider,
			IPluginStateProvider pluginStateProvider,
			IKibanaEnvironmentStateProvider environmentStateProvider,
			ISession session,
			string[] args
		) : base(wixStateProvider, session, args)
		{
			var versionConfig = new VersionConfiguration(wixStateProvider, this.Session.IsInstalled);
			this.KibanaEnvironmentState = environmentStateProvider;

			this.LocationsModel = new LocationsModel(versionConfig);
			this.NoticeModel = new NoticeModel(versionConfig, serviceStateProvider, this.LocationsModel);
			this.ServiceModel = new ServiceModel(serviceStateProvider, versionConfig);
			this.ConfigurationModel = new ConfigurationModel();
			this.ConnectingModel = new ConnectingModel();
			var pluginDependencies = this.WhenAnyValue(
				vm => vm.NoticeModel.AlreadyInstalled,
				vm => vm.LocationsModel.InstallDir,
				vm => vm.LocationsModel.ConfigDirectory
			);
			this.PluginsModel = new PluginsModel(pluginStateProvider, pluginDependencies);

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;
			var observeHost = this.WhenAnyValue(x => x.ConfigurationModel.HostName, x => x.ConfigurationModel.HttpPort,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeInstallationLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);
			var observeKibanaLog = this.WhenAnyValue(vm => vm.LocationsModel.KibanaLog);
			var observeInstallXPack = this.PluginsModel.AvailablePlugins.ItemChanged
				.Where(x => x.PropertyName == nameof(Plugin.Selected) && x.Sender.PluginType == PluginType.XPack)
				.Select(x => x.Sender.Selected);

			this.ClosingModel = new ClosingModel(wixStateProvider.InstallerVersion, isUpgrade, observeHost, observeInstallationLog, observeKibanaLog, serviceStateProvider);

			this.AllSteps.AddRange(new List<IStep>
			{
				this.NoticeModel,
				this.LocationsModel,
				this.ServiceModel,
				this.ConfigurationModel,
				this.ConnectingModel,
				this.PluginsModel,
				this.ClosingModel
			});

			var observeValidationChanges = this.WhenAny(
				vm => vm.NoticeModel.ValidationFailures,
				vm => vm.LocationsModel.ValidationFailures,
				vm => vm.PluginsModel.ValidationFailures,
				vm => vm.ServiceModel.ValidationFailures,
				vm => vm.ConfigurationModel.ValidationFailures,
				vm => vm.ConnectingModel.ValidationFailures,
				vm => vm.ClosingModel.ValidationFailures,
				vm => vm.TabSelectedIndex,
				(notice, locations, plugins, service, config, connecting, closing, index) =>
				{
					var firstInvalidScreen = this.Steps.FirstOrDefault(s => !s.IsValid) ?? this.ClosingModel;
					return firstInvalidScreen;
				});
			observeValidationChanges
				.Subscribe(selected =>
				{
					var step = this.Steps[this.TabSelectedIndex];
					var failures = step.ValidationFailures;
					this.FirstInvalidStepValidationFailures = selected.ValidationFailures;
				});

			this.WhenAny(
					vm => vm.NoticeModel.IsValid,
					vm => vm.LocationsModel.IsValid,
					vm => vm.PluginsModel.IsValid,
					vm => vm.ServiceModel.IsValid,
					vm => vm.ConfigurationModel.IsValid,
					vm => vm.ConnectingModel.IsValid,
					vm => vm.ClosingModel.IsValid,
					(notice, locations, plugins, service, config, connecting, closing) =>
					{
						var firstInvalidScreen = this.Steps.Select((s, i) => new {s, i}).FirstOrDefault(s => !s.s.IsValid);
						return firstInvalidScreen?.i ?? (this.Steps.Count - 1);
					})
				.Subscribe(selected =>
				{
					this.TabSelectionMax = selected;
					//if one of the steps prior to the current selection is invalid jump back
					if (this.TabSelectedIndex > this.TabSelectionMax)
						this.TabSelectedIndex = this.TabSelectionMax;

					this.FirstInvalidStepValidationFailures = this.ActiveStep.ValidationFailures;
				});

			this.Install = ReactiveCommand.CreateAsyncTask(observeValidationChanges.Select(s => s.IsValid), _ =>
			{
				this.TabSelectedIndex += 1;
				return this.InstallUITask();
			});
			this.Install.Subscribe(installationObservable =>
			{
				installationObservable.Subscribe(installed => { this.ClosingModel.Installed = installed; });
			});

			this.Refresh();
			//validate the first stab explicitly on constructing this
			//main viewmodel. WPF triggers a validation already

			this.ParsedArguments = new KibanaArgumentParser(
				this.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] {this}).ToList(), args);

			this.ActiveStep.Validate();
		}

		public static KibanaInstallationModel Create(
			IWixStateProvider wixState,
			ISession session,
			params string[] args
		)
		{
			var serviceState = ServiceStateProvider.FromSession(session, "Kibana");
			var pluginState = PluginStateProviderBase.KibanaDefault(session);
			var envState = KibanaEnvironmentStateProvider.Default;
			return new KibanaInstallationModel(wixState, serviceState, pluginState, envState, session, args);
		}

		public override void Refresh()
		{
			foreach (var step in this.Steps) step.Refresh();
			this.MsiLogFileLocation = this.Session.Get<string>("MsiLogFileLocation");
		}

		public KibanaServiceConfiguration GetServiceConfiguration()
		{
			var service = this.ServiceModel;
			var automaticStart = service.StartWhenWindowsStarts;
			var configuration = new KibanaServiceConfiguration
			{
				Name = "Kibana",
				DisplayName = "Kibana",
				Description = "You know, for Search.",
				StartMode = automaticStart ? ServiceStartMode.Automatic : ServiceStartMode.Manual,
				EventLogSource = "Kibana",
				HomeDirectory = this.LocationsModel.InstallDir,
				ConfigDirectory = this.LocationsModel.ConfigDirectory,
				ExeLocation = Path.Combine(this.LocationsModel.InstallDir, "bin", "kibana.exe")
			};
			var username = service.User;

			var password = service.Password;
			if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
			{
				configuration.ServiceAccount = ServiceAccount.User;
				configuration.UserName = username;
				configuration.Password = password;
			}
			else if (service.UseNetworkService)
				configuration.ServiceAccount = ServiceAccount.NetworkService;
			else
				configuration.ServiceAccount = ServiceAccount.LocalSystem;
			return configuration;
		}
	}
}