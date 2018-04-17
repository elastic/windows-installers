﻿using System;
using System.Text;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Properties;
using ReactiveUI;
using Semver;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Notice
{
	public class NoticeModel : StepBase<NoticeModel, NoticeModelValidator>
	{
		public NoticeModel(
			VersionConfiguration versionConfig, 
			IServiceStateProvider serviceStateProvider, 
			LocationsModel locationsModel,
			ServiceModel serviceModel
			)
		{
			this.IsRelevant = versionConfig.ExistingVersionInstalled;
			this.LocationsModel = locationsModel;
			this.ServiceModel = serviceModel;
			this.Header = "Notice";
			this.ExistingVersion = versionConfig.PreviousVersion;
			this.CurrentVersion = versionConfig.InstallerVersion;
			this.ReadMoreOnUpgrades = ReactiveCommand.Create();
			this.ReadMoreOnXPackOpening = ReactiveCommand.Create();

			if (!string.IsNullOrWhiteSpace(this.CurrentVersion?.Prerelease))
			{
				if (versionConfig.ExistingVersionInstalled)
				{
					this.UpgradeTextHeader = TextResources.NoticeModel_ToPrerelease_Header;
					this.UpgradeText = TextResources.NoticeModel_ToPrerelease;
				}
				else
				{
					this.UpgradeTextHeader = TextResources.NoticeModel_Prerelease_Header;
					this.UpgradeText = TextResources.NoticeModel_Prerelease;
				}
				this.IsRelevant = true; //show prerelease notice always
			}
			else if (!string.IsNullOrWhiteSpace(this.ExistingVersion?.Prerelease))
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
			if (!string.IsNullOrWhiteSpace(this.UpgradeTextHeader))
				this.UpgradeTextHeader = string.Format(this.UpgradeTextHeader, versionConfig.PreviousVersion, versionConfig.InstallerVersion);

			this.ShowOpeningXPackBanner = this.ExistingVersion < "6.3.0";
			this.ShowUpgradeDocumentationLink = versionConfig.VersionChange == VersionChange.Major || versionConfig.VersionChange == VersionChange.Minor;
			
			this.ExistingVersionInstalled = versionConfig.ExistingVersionInstalled;
			this.InstalledAsService = serviceStateProvider.SeesService;
			this.Refresh();
		}
		
		bool showUpgradeDocumentationLink;
		public bool ShowUpgradeDocumentationLink
		{
			get => showUpgradeDocumentationLink;
			private set => this.RaiseAndSetIfChanged(ref showUpgradeDocumentationLink, value);
		}

		bool showOpeningXPackBanner;
		public bool ShowOpeningXPackBanner
		{
			get => showOpeningXPackBanner;
			private set => this.RaiseAndSetIfChanged(ref showOpeningXPackBanner, value);
		}

		bool? existingVersionInstalled;
		public bool ExistingVersionInstalled
		{
			get => existingVersionInstalled.GetValueOrDefault();
			private set => this.RaiseAndSetIfChanged(ref existingVersionInstalled, value);
		}

		bool? installedAsService;
		public bool InstalledAsService
		{
			get => installedAsService.GetValueOrDefault();
			private set => this.RaiseAndSetIfChanged(ref installedAsService, value);
		}

		public sealed override void Refresh() { }

		public ServiceModel ServiceModel { get; }
		public LocationsModel LocationsModel { get; }
		public SemVersion CurrentVersion { get; }
		public SemVersion ExistingVersion { get; }
		public string UpgradeTextHeader { get; }
		public string UpgradeText { get; }
		public ReactiveCommand<object> ReadMoreOnUpgrades { get; }
		public ReactiveCommand<object> ReadMoreOnXPackOpening { get; }

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(NoticeModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			return sb.ToString();
		}

	}
}