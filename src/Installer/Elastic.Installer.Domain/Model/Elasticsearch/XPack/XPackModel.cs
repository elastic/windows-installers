﻿using System;
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
			});
			canAutomaticallySetupUsers.Subscribe(b=>
			{
				this.CanAutomaticallySetupUsers = b;
				this.SkipSettingPasswords = !b;
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
			}
		}

		public bool NeedsPasswords =>
			this.IsRelevant 
			&& this.CanAutomaticallySetupUsers 
			&& this.XPackLicense == XPackLicenseMode.Trial
			&& !this.SkipSettingPasswords
			&& this.XPackSecurityEnabled;
		
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
			sb.AppendLine($"- {nameof(this.XPackSecurityEnabled)} = " + SkipSettingPasswords);
			sb.AppendLine($"- {nameof(this.ElasticUserPassword)} = " + !string.IsNullOrWhiteSpace(ElasticUserPassword));
			sb.AppendLine($"- {nameof(this.KibanaUserPassword)} = " + !string.IsNullOrWhiteSpace(KibanaUserPassword));
			sb.AppendLine($"- {nameof(this.LogstashSystemUserPassword)} = " + !string.IsNullOrWhiteSpace(LogstashSystemUserPassword));
			return sb.ToString();
		}

	}
}