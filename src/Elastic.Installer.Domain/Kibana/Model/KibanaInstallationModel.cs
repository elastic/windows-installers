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

namespace Elastic.Installer.Domain.Kibana.Model
{
	public class KibanaInstallationModel
		: InstallationModelBase<KibanaInstallationModel, KibanaInstallationModelValidator>
	{
		public LocationsModel LocationsModel { get; }
		public ServiceModel ServiceModel { get; }
		public ConfigurationModel ConfigurationModel { get; }
		public ConnectingModel ConnectingModel { get; }
		public PluginsModel PluginsModel { get; }
		public ClosingModel ClosingModel { get; }

		public override ReactiveList<IStep> AllSteps => new ReactiveList<IStep>
			{
				this.LocationsModel,
				this.ServiceModel,
				this.ConfigurationModel,
				this.ConnectingModel,
				this.PluginsModel,
				this.ClosingModel
			};

		public override IObservable<IStep> ObserveValidationChanges => this.WhenAny(
			vm => vm.LocationsModel.ValidationFailures,
			vm => vm.PluginsModel.ValidationFailures,
			vm => vm.ServiceModel.ValidationFailures,
			vm => vm.ConfigurationModel.ValidationFailures,
			vm => vm.ConnectingModel.ValidationFailures,
			vm => vm.ClosingModel.ValidationFailures,
			vm => vm.TabSelectedIndex,
			(locations, plugins, service, config, connecting, closing, index) =>
			{
				var firstInvalidScreen = this.Steps.FirstOrDefault(s => !s.IsValid) ?? this.ClosingModel;
				return firstInvalidScreen;
			});

		public KibanaInstallationModel(
			IWixStateProvider wixStateProvider,
			IServiceStateProvider serviceStateProvider,
			IPluginStateProvider pluginStateProvider,
			ISession session,
			string[] args
			) : base(wixStateProvider, session, args)
		{
			var versionConfig = new VersionConfiguration(wixStateProvider);

			this.LocationsModel = new LocationsModel(versionConfig);
			this.ServiceModel = new ServiceModel(serviceStateProvider, versionConfig);
			this.ConfigurationModel = new ConfigurationModel();
			this.ConnectingModel = new ConnectingModel();
			this.PluginsModel = new PluginsModel(pluginStateProvider);

			var observeHost = this.WhenAnyValue(x => x.ConfigurationModel.HostName, x => x.ConfigurationModel.HttpPort,
				(h, p) => $"http://{(string.IsNullOrWhiteSpace(h) ? "localhost" : h)}:{p}");
			var observeLog = this.WhenAnyValue(vm => vm.MsiLogFileLocation);

			var isUpgrade = versionConfig.InstallationDirection == InstallationDirection.Up;
			this.ClosingModel = new ClosingModel(wixStateProvider.CurrentVersion, isUpgrade, observeHost, observeLog, serviceStateProvider);

			this.Install.Subscribe(installationObservable =>
			{
				installationObservable.Subscribe(installed =>
				{
					this.ClosingModel.Installed = installed;
				});
			});

			this.WhenAny(
				vm => vm.LocationsModel.IsValid,
				vm => vm.PluginsModel.IsValid,
				vm => vm.ServiceModel.IsValid,
				vm => vm.ConfigurationModel.IsValid,
				vm => vm.ConnectingModel.IsValid,
				vm => vm.ClosingModel.IsValid,
				(locations, plugins, service, config, connecting, closing) =>
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

			this.ParsedArguments = new KibanaInstallationModelArgumentParser(this.AllSteps.Cast<IValidatableReactiveObject>().Concat(new[] { this }).ToList(), args);

			this.ActiveStep.Validate();
		}

		public static KibanaInstallationModel Create(
			IWixStateProvider wixState,
			ISession session,
			params string[] args
			)
		{
			var serviceState = ServiceStateProvider.FromSession(session);
			var pluginState = PluginStateProvider.Default;
			return new KibanaInstallationModel(wixState, serviceState, pluginState, session, args);
		}

		public override void Refresh()
		{
			foreach (var step in this.Steps) step.Refresh();
			this.MsiLogFileLocation = this.Session.Get<string>("MsiLogFileLocation");
		}
	}
}
