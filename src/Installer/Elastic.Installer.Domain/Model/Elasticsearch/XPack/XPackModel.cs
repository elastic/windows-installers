using System;
using System.Text;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Model.Base;
using ReactiveUI;
using Semver;
using static Elastic.Installer.Domain.Configuration.Wix.InstallationDirection;

namespace Elastic.Installer.Domain.Model.Elasticsearch.XPack
{
	public class XPackModel : StepBase<XPackModel, XPackModelValidator>
	{
		public const XPackLicenseMode DefaultXPackLicenseMode = XPackLicenseMode.Basic;
		public static readonly SemVersion XPackInstalledByDefaultVersion = "6.3.0";

		public static readonly string DefaultElasticUserPassword = null;
		public static readonly string DefaultKibanaUserPassword = null;
		public static readonly string DefaultLogstashSystemUserPassword = null;

		public XPackModel(VersionConfiguration versionConfig,
			IObservable<bool> canAutomaticallySetupUsers,
			IObservable<bool> upgradeFromXPackPlugin)
		{
			upgradeFromXPackPlugin.Subscribe(b =>
			{
				//show x-pack tab when we're upgrading from a version lower than `6.3.0` and the x-pack plugin was not installed.
				//otherwise assume x-pack is installed and only show the tab on new installs
				this.IsRelevant = 
					(versionConfig.InstallationDirection == Up && versionConfig.UpgradeFromVersion < XPackInstalledByDefaultVersion && !b) 
					|| versionConfig.InstallationDirection == None;
			});
			
			canAutomaticallySetupUsers.Subscribe(b=>
			{
				this.CanAutomaticallySetupUsers = b;
				this.SkipSettingPasswords = !b;
			});
			this.Header = "X-Pack";
			this.CurrentVersion = versionConfig.CurrentVersion;
			this.OpenLicensesAndSubscriptions = ReactiveCommand.Create();
			this.OpenManualUserConfiguration = ReactiveCommand.Create();
			this.Refresh();
		}

		public sealed override void Refresh()
		{
			this.ElasticUserPassword = DefaultElasticUserPassword;
			this.KibanaUserPassword = DefaultKibanaUserPassword;
			this.LogstashSystemUserPassword = DefaultLogstashSystemUserPassword;
			this.BootstrapPassword = null;
			this.XPackSecurityEnabled = false;
			this.XPackLicense = DefaultXPackLicenseMode;
		}

		public SemVersion CurrentVersion { get; }
		
		string elasticUserPassword;
		[Argument(nameof(ElasticUserPassword), IsHidden = true)]
		public string ElasticUserPassword
		{
			get => this.elasticUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.elasticUserPassword, value);
		}
		string kibanaUserPassword;
		[Argument(nameof(KibanaUserPassword), IsHidden = true)]
		public string KibanaUserPassword
		{
			get => this.kibanaUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.kibanaUserPassword, value);
		}
		string logstashSystemUserPassword;
		[Argument(nameof(LogstashSystemUserPassword), IsHidden = true)]
		public string LogstashSystemUserPassword
		{
			get => this.logstashSystemUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.logstashSystemUserPassword, value);
		}
		
		bool canAutomaticallySetupUsers;
		public bool CanAutomaticallySetupUsers
		{
			get => this.canAutomaticallySetupUsers;
			set => this.RaiseAndSetIfChanged(ref this.canAutomaticallySetupUsers, value);
		}
		
		bool xPackSecurityEnabled;
		[Argument(nameof(XPackSecurityEnabled))]
		public bool XPackSecurityEnabled
		{
			get => this.xPackSecurityEnabled;
			set => this.RaiseAndSetIfChanged(ref this.xPackSecurityEnabled, value);
		}
		
		bool skipSettingPasswords;
		[Argument(nameof(SkipSettingPasswords))]
		public bool SkipSettingPasswords
		{
			get => this.skipSettingPasswords;
			set => this.RaiseAndSetIfChanged(ref this.skipSettingPasswords, value);
		}

		XPackLicenseMode xPackLicense;
		[Argument(nameof(XPackLicense))]
		public XPackLicenseMode XPackLicense
		{
			get => this.xPackLicense;
			set
			{
				this.RaiseAndSetIfChanged(ref this.xPackLicense, value);
				if (value == XPackLicenseMode.Trial)
					this.XPackSecurityEnabled = true;
			}
		}

		private string bootstrapPassword;
		[Argument(nameof(BootstrapPassword), IsHidden = true)]
		public string BootstrapPassword
		{
			get => this.bootstrapPassword;
			set => this.RaiseAndSetIfChanged(ref this.bootstrapPassword, value);
		}

		public bool NeedsPasswords =>
			this.IsRelevant 
			&& this.CanAutomaticallySetupUsers 
			&& this.XPackLicense == XPackLicenseMode.Trial
			&& !this.SkipSettingPasswords
			&& this.XPackSecurityEnabled;

		public ReactiveCommand<object> OpenLicensesAndSubscriptions { get; }

		public ReactiveCommand<object> OpenManualUserConfiguration { get; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(XPackModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(IsRelevant)} = " + IsRelevant);
			sb.AppendLine($"- {nameof(NeedsPasswords)} = " + NeedsPasswords);
			sb.AppendLine($"- {nameof(XPackLicense)} = " + Enum.GetName(typeof(XPackLicenseMode), XPackLicense));
			sb.AppendLine($"- {nameof(this.CanAutomaticallySetupUsers)} = " + CanAutomaticallySetupUsers);
			sb.AppendLine($"- {nameof(this.SkipSettingPasswords)} = " + SkipSettingPasswords);
			sb.AppendLine($"- {nameof(this.XPackSecurityEnabled)} = " + XPackSecurityEnabled);
			sb.AppendLine($"- {nameof(this.BootstrapPassword)} = " + !string.IsNullOrWhiteSpace(BootstrapPassword));
			sb.AppendLine($"- {nameof(this.ElasticUserPassword)} = " + !string.IsNullOrWhiteSpace(ElasticUserPassword));
			sb.AppendLine($"- {nameof(this.KibanaUserPassword)} = " + !string.IsNullOrWhiteSpace(KibanaUserPassword));
			sb.AppendLine($"- {nameof(this.LogstashSystemUserPassword)} = " + !string.IsNullOrWhiteSpace(LogstashSystemUserPassword));
			return sb.ToString();
		}

	}
}