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
		public const XPackLicenseMode DefaultXPackLicenseMode = XPackLicenseMode.Trial;
		
		public XPackModel(IObservable<bool> xPackEnabled)
		{
			xPackEnabled.Subscribe(t =>
			{
				this._xPackLicenseDefault = t ? DefaultXPackLicenseMode : (XPackLicenseMode?)null;
				this.IsRelevant = t;
				this.IsVisible = t;
				this.Refresh();
			});
			this.Header = "X-Pack";
			this.Refresh();
		}

		public sealed override void Refresh()
		{
			this.XPackLicense = this._xPackLicenseDefault;
			this.XPackUsername = "elastic";
			this.XPackUserPassword = null;
		}
		
		string _xPackUsername;
		[StaticArgument(nameof(_xPackUsername))]
		public string XPackUsername
		{
			get => this._xPackUsername;
			set => this.RaiseAndSetIfChanged(ref this._xPackUsername, value);
		}
		string xPackUserPassword;
		[StaticArgument(nameof(xPackUserPassword))]
		public string XPackUserPassword
		{
			get => this.xPackUserPassword;
			set => this.RaiseAndSetIfChanged(ref this.xPackUserPassword, value);
		}

		XPackLicenseMode? _xPackLicenseDefault;
		XPackLicenseMode? xPackLicense;
		[StaticArgument(nameof(XPackLicense))]
		public XPackLicenseMode? XPackLicense
		{
			get => this.xPackLicense;
			set => this.RaiseAndSetIfChanged(ref this.xPackLicense, value);
		}
		
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