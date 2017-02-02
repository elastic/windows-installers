using Elastic.Installer.Domain.Kibana.Model;
using Elastic.Installer.Domain.Kibana.Model.Closing;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Session;
using Elastic.Installer.Domain.Shared.Configuration;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Model.Plugins;
using Elastic.Installer.Domain.Shared.Model.Service;
using Elastic.Installer.Domain.Kibana.Model.Locations;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Kibana.Model.Configuration;
using Elastic.Installer.Domain.Kibana.Model.Connecting;
using Elastic.Installer.Domain.Kibana.Model.Plugins;
using Elastic.Installer.Domain.Kibana.Model.Notice;
using Elastic.Installer.Domain.Kibana.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Service.Kibana;
using System.ServiceProcess;
using System.IO;

namespace Elastic.Installer.Domain.Kibana.Model
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

		public override IObservable<IStep> ObserveValidationChanges => this.WhenAny(
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

		public KibanaInstallationModel(
			IWixStateProvider wixStateProvider,
			IServiceStateProvider serviceStateProvider,
			IPluginStateProvider pluginStateProvider,
			IKibanaEnvironmentStateProvider environmentStateProvider,
			ISession session,
			string[] args
			) : base(wixStateProvider, session, args)
		{
			var versionConfig = new VersionConfiguration(wixStateProvider);
			this.KibanaEnvironmentState = environmentStateProvider;

			this.LocationsModel = new LocationsModel(versionConfig);
			this.NoticeModel = new NoticeModel(versionConfig, serviceStateProvider, this.LocationsModel);
			this.ServiceModel = new ServiceModel(serviceStateProvider, versionConfig);
			this.ConfigurationModel = new ConfigurationModel();
			this.ConnectingModel = new ConnectingModel();
			this.PluginsModel = new PluginsModel(pluginStateProvider);	

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;
			var observeHost = this.WhenAnyValue(x => x.ConfigurationModel.HostName, x => x.ConfigurationModel.HttpPort,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeInstallationLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);
			var observeKibanaLog = this.WhenAnyValue(vm => vm.LocationsModel.KibanaLog);
			this.ClosingModel = new ClosingModel(wixStateProvider.CurrentVersion, isUpgrade, observeHost, observeInstallationLog, observeKibanaLog, serviceStateProvider);

			this.AllSteps = new ReactiveList<IStep>
			{
				this.NoticeModel,
				this.LocationsModel,
				this.ServiceModel,
				this.ConfigurationModel,
				this.ConnectingModel,
				this.PluginsModel,
				this.ClosingModel
			};
			this.Steps = this.AllSteps.CreateDerivedCollection(x => x, x => x.IsRelevant);

			this.Install.Subscribe(installationObservable =>
			{
				installationObservable.Subscribe(installed =>
				{
					this.ClosingModel.Installed = installed;
				});
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
					var firstInvalidScreen = this.Steps.Select((s, i) => new { s, i }).FirstOrDefault(s => !s.s.IsValid);
					return firstInvalidScreen?.i ?? (this.Steps.Count - 1);
				})
				.Subscribe(selected =>
				{
					this.TabSelectionMax = selected;
					//if one of the steps prior to the current selection is invalid jump back
					if (this.TabSelectedIndex > this.TabSelectionMax)
						this.TabSelectedIndex = this.TabSelectionMax;

					this.CurrentStepValidationFailures = this.ActiveStep.ValidationFailures;
				});

			this.Refresh();
			//validate the first stab explicitly on constructing this 
			//main viewmodel. WPF triggers a validation already	

			this.ParsedArguments = new KibanaArgumentParser(this.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] { this }).ToList(), args);

			this.ActiveStep.Validate();
		}

		public static KibanaInstallationModel Create(
			IWixStateProvider wixState,
			ISession session,
			params string[] args
			)
		{
			var serviceState = ServiceStateProvider.FromSession(session, "Kibana");
			var pluginState = PluginStateProvider.Default;
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
