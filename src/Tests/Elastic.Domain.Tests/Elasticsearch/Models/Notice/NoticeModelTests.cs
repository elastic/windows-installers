using Elastic.Installer.Domain.Properties;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Notice
{
	public class NoticeModelTests : InstallationModelTestBase
	{
		[Fact]
		public void ErrorStates() => Pristine()
			.HasSetupValidationFailures(errors => errors
				.ShouldHaveErrors(
					TextResources.NoticeModelValidator_JavaInstalled
				)
			)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaInstalled)
			);

		[Fact]
		public void CanClickNextWhenValid() => DefaultValidModel()
			.IsValidOnStep(m => m.LocationsModel)
			.CanClickNext();

		[Fact]
		public void MajorDowngradeShowsNotice() => DefaultValidModel(f => f.Wix("5.0.0", upgradeFrom: "6.0.0"))
			.HasSetupValidationFailures(e => e.ShouldHaveErrors(TextResources.NoticeModelValidator_HigherVersionInstalled));

		[Fact] public void MajorUpgradeShowsNotice() => DefaultValidModel(f=>f.Wix("6.0.0", upgradeFrom: "5.0.0"))
			.IsValidOnStep(m => m.NoticeModel)
			.CanClickNext();

		[Fact] public void MinorDowngradeShowsNotice() => DefaultValidModel(f=>f.Wix("5.0.0", upgradeFrom: "5.1.0"))
			.HasSetupValidationFailures(e => e.ShouldHaveErrors(TextResources.NoticeModelValidator_HigherVersionInstalled));

		[Fact] public void MinorUpgradeShowsNotice() => DefaultValidModel(f=>f.Wix("5.1.0", upgradeFrom: "5.0.0"))
			.IsValidOnStep(m => m.NoticeModel)
			.CanClickNext();

		[Fact] public void PatchDowngradeShowsNotice() => DefaultValidModel(f=>f.Wix("5.1.0", upgradeFrom: "5.1.1"))
			.HasSetupValidationFailures(e => e.ShouldHaveErrors(TextResources.NoticeModelValidator_HigherVersionInstalled));

		[Fact] public void PatchUpgradeDoesNotShowsNotice() => DefaultValidModel(f=>f.Wix("5.1.1", upgradeFrom: "5.1.0"))
			.IsValidOnStep(m => m.NoticeModel)
			.CanClickNext();

		[Fact] public void PatchUpgradeNextScreenIsConfigurationScreen() => DefaultValidModel(f=>f
				.Wix("5.1.1", upgradeFrom: "5.1.0")
				.ServicePreviouslyInstalled()
			)
			.IsValidOnStep(m => m.NoticeModel)
			.ClickNext()
			.IsValidOnStep(m => m.ConfigurationModel)
			.CanClickNext();

		[Fact] public void PrereleaseShowsNotice() => DefaultValidModel(f=>f.Wix("5.0.0-alpha4", upgradeFrom: "5.0.0-alpha2"))
			.IsValidOnStep(m => m.NoticeModel)
			.CanClickNext();

		[Fact] public void PrereleaseDowngradeShowsNotice() => DefaultValidModel(f=>f.Wix("5.0.0-rc2", upgradeFrom: "5.0.0"))
			.HasSetupValidationFailures(e => e.ShouldHaveErrors(TextResources.NoticeModelValidator_HigherVersionInstalled));

		[Fact] public void PrereleaseUpgradeShowsNotice() => DefaultValidModel(f=>f.Wix("5.0.0-rc2", upgradeFrom: "5.0.0"))
			.HasSetupValidationFailures(e => e.ShouldHaveErrors(TextResources.NoticeModelValidator_HigherVersionInstalled));

		[Fact] public void InstallingPrereleaseFirstTimeShowsNotice() => DefaultValidModel(f=>f.Wix("5.0.0-beta2", upgradeFrom: null))
			.IsValidOnStep(m => m.NoticeModel)
			.CanClickNext();
	}
}
