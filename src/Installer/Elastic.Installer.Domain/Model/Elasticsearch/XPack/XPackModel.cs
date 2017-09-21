using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elastic.Configuration.FileBased.JvmOpts;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Model.Base;
using Microsoft.VisualBasic.Devices;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Elasticsearch.XPack
{
	public class XPackModel : StepBase<XPackModel, XPackModelValidator>
	{
		public const XPackLicenseMode DefaultXPackLicenseMode = XPackLicenseMode.Basic;
		
		public XPackModel(IObservable<bool> xPackEnabled, IObservable<bool> canAutomaticallySetupUsers)
		{
			xPackEnabled.Subscribe(t =>
			{
				this.IsRelevant = t;
				this.XPackLicense = !t ? (XPackLicenseMode?)null : _lastSetXpackLicenseMode;
			});
			canAutomaticallySetupUsers.Subscribe(b=>
			{
				this.CanAutomaticallySetupUsers = b;
				this.GenerateUsersLater = !b;
			});
			this.Header = "X-Pack";
			this.Refresh();
		}

		public sealed override void Refresh()
		{
			this.ElasticUserPassword = null;
			this.KibanaUserPassword = null;
			this.LogstashSystemUserPassword = null;
			this.XPackSecurityEnabled = true;
		}
		
		string elasticUserPassword;
		[StaticArgument(nameof(ElasticUserPassword), IsHidden = true)]
		public string ElasticUserPassword
		{
			get => this.elasticUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.elasticUserPassword, value);
		}
		string kibanaUserPassword;
		[StaticArgument(nameof(KibanaUserPassword), IsHidden = true)]
		public string KibanaUserPassword
		{
			get => this.kibanaUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.kibanaUserPassword, value);
		}
		string logstashSystemUserPassword;
		[StaticArgument(nameof(LogstashSystemUserPassword), IsHidden = true)]
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
		[StaticArgument(nameof(XPackSecurityEnabled))]
		public bool XPackSecurityEnabled
		{
			get => this.xPackSecurityEnabled;
			set => this.RaiseAndSetIfChanged(ref this.xPackSecurityEnabled, value);
		}
		
		bool generateUsersLater;
		[StaticArgument(nameof(GenerateUsersLater))]
		public bool GenerateUsersLater
		{
			get => this.generateUsersLater;
			set => this.RaiseAndSetIfChanged(ref this.generateUsersLater, value);
		}

		XPackLicenseMode _lastSetXpackLicenseMode = DefaultXPackLicenseMode;
		XPackLicenseMode? xPackLicense;
		[StaticArgument(nameof(XPackLicense))]
		public XPackLicenseMode? XPackLicense
		{
			get => this.xPackLicense;
			set
			{
				if (value.HasValue) this._lastSetXpackLicenseMode = value.Value;
				this.RaiseAndSetIfChanged(ref this.xPackLicense, value);
			}
		}

		public bool NeedsPassword =>
			this.IsRelevant 
			&& this.CanAutomaticallySetupUsers 
			&& this.XPackLicense == XPackLicenseMode.Trial
			&& !this.GenerateUsersLater
			&& this.XPackSecurityEnabled;
		
		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(XPackModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			if (XPackLicense.HasValue)
				sb.AppendLine($"- {nameof(XPackLicense)} = " + Enum.GetName(typeof(XPackLicenseMode), XPackLicense.Value));
			return sb.ToString();
		}

	}
}