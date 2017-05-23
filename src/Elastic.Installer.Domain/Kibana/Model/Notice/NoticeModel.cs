using System;
using System.Resources;
using System.Text;
using ReactiveUI;
using Semver;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Kibana.Model.Locations;
using Elastic.Installer.Domain.Model;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Configuration;

namespace Elastic.Installer.Domain.Kibana.Model.Notice
{
	public class NoticeModel : StepBase<NoticeModel, NoticeModelValidator>
	{
		public NoticeModel(
			VersionConfiguration versionConfig, 
			IServiceStateProvider serviceStateProvider, 
			LocationsModel locationsModel
			)
		{
			this.IsRelevant = versionConfig.AlreadyInstalled;
			this.LocationsModel = locationsModel;
			this.Header = "Notice";
			this.ExistingVersion = versionConfig.ExistingVersion;
			this.CurrentVersion = versionConfig.CurrentVersion;
			this.ReadMoreOnUpgrades = ReactiveCommand.Create();

			var e = versionConfig.ExistingVersion;
			var c = versionConfig.CurrentVersion;
			if (!string.IsNullOrWhiteSpace(c?.Prerelease))
			{
				this.UpgradeTextHeader = TextResources.NoticeModel_ToPrerelease_Header;
				this.UpgradeText = TextResources.NoticeModel_ToPrerelease;
				this.IsRelevant = true; //show prerelease notice always

			}
			else if (!string.IsNullOrWhiteSpace(e?.Prerelease))
			{
				this.UpgradeTextHeader = TextResources.NoticeModel_FromPrerelease_Header;
				this.UpgradeText = TextResources.NoticeModel_FromPrerelease;
				this.IsRelevant = true; //show prerelease notice always
			}
			else
			{
				var v = Enum.GetName(typeof(VersionChange), versionConfig.VersionChange);
				var d = Enum.GetName(typeof(InstallationDirection), versionConfig.InstallationDirection);
				var prefix = nameof(NoticeModel) + "_" + v + d;
				this.UpgradeTextHeader = TextResources.ResourceManager.GetString(prefix + "_Header");
				this.UpgradeText = TextResources.ResourceManager.GetString(prefix);
			}
			if (this.IsRelevant
				&& versionConfig.VersionChange == VersionChange.Patch
				&& versionConfig.InstallationDirection == InstallationDirection.Up)
				this.IsRelevant = false;

			this.AlreadyInstalled = versionConfig.AlreadyInstalled;
			this.InstalledAsService = serviceStateProvider.SeesService;
			this.Refresh();
		}


		bool? alreadyInstalled;
		public bool AlreadyInstalled
		{
			get => alreadyInstalled.GetValueOrDefault();
			private set => this.RaiseAndSetIfChanged(ref alreadyInstalled, value);
		}

		bool? installedAsService;
		public bool InstalledAsService
		{
			get => installedAsService.GetValueOrDefault();
			private set => this.RaiseAndSetIfChanged(ref installedAsService, value);
		}

		public sealed override void Refresh() { }

		public LocationsModel LocationsModel { get; }

		public SemVersion CurrentVersion { get; }
		public SemVersion ExistingVersion { get; }
		public string UpgradeTextHeader { get; }
		public string UpgradeText { get; }
		public ReactiveCommand<object> ReadMoreOnUpgrades { get; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(NoticeModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			return sb.ToString();
		}

	}
}