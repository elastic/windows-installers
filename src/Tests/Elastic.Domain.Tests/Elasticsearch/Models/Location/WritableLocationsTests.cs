using System.IO;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Location
{
	public class WritableLocationsTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;
		private string _installDirectory = "C:\\elasticsearch";
		private string VersionSpecificInstallDirectory => Path.Combine(this._installDirectory, TestSetupStateProvider.DefaultTestVersion);

		public WritableLocationsTests()
		{
			this._model = WithValidPreflightChecks()
				.IsValidOnStep(m => m.LocationsModel);
		}

		[Fact] void WhenSettingsSamePathWeDoNotAcceptProgramFiles() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.PlaceWritableLocationsInSamePath = true;
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.LogsText),
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.DataText),
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.ConfigurationText)
				)
			)
			.CanClickNext(false);

		[Fact] void UnsettingsPlaceInSamePathLeavesWritableFoldersInErrorState() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.PlaceWritableLocationsInSamePath = true;
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.LogsText),
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.DataText),
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.ConfigurationText)
				)
			)
			.OnStep(m => m.LocationsModel, step =>
			{
				//eventhough we unset place writable folders in the same path, the folders do not change
				step.PlaceWritableLocationsInSamePath = false;
				step.LogsDirectory.Should().StartWith(step.InstallDir)
					.And.StartWith(LocationsModel.DefaultProductInstallationDirectory);
				step.ConfigDirectory.Should().StartWith(step.InstallDir)
					.And.StartWith(LocationsModel.DefaultProductInstallationDirectory);
				step.DataDirectory.Should().StartWith(step.InstallDir)
					.And.StartWith(LocationsModel.DefaultProductInstallationDirectory);;
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.LogsText),
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.DataText),
					string.Format(LocationsModelValidator.DirectorySetToNonWritableLocation, LocationsModelValidator.ConfigurationText)
				)
			)
			.CanClickNext(false);

		[Fact] void WhenSettingWritableFoldersWeCanAdvance() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = _installDirectory;
				step.PlaceWritableLocationsInSamePath = true;
			})
			.IsValidOnStep(m => m.LocationsModel)
			.CanClickNext();

		[Fact] void WhenSettingWritableFoldersOutOfOrderWeCanAdvance() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.PlaceWritableLocationsInSamePath = true;
				step.InstallDir = _installDirectory;
			})
			.IsValidOnStep(m => m.LocationsModel)
			.CanClickNext();
			
		[Fact] void WithSameDirFlagSetManuallySettingWritableDirectoriesElsewhereIsAnError() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.PlaceWritableLocationsInSamePath = true;
				step.InstallDir = _installDirectory;
			})
			.IsValidOnStep(m=>m.LocationsModel)
			.OnStep(m => m.LocationsModel, step =>
			{
				step.LogsDirectory = "C:\\elsewhere";
				step.DataDirectory = "C:\\elsewhere";
				step.ConfigDirectory = "C:\\elsewhere";
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(
					string.Format(LocationsModelValidator.DirectoryMustBeChildOf, LocationsModelValidator.LogsText, VersionSpecificInstallDirectory),
					string.Format(LocationsModelValidator.DirectoryMustBeChildOf, LocationsModelValidator.DataText, VersionSpecificInstallDirectory),
					string.Format(LocationsModelValidator.DirectoryMustBeChildOf, LocationsModelValidator.ConfigurationText, VersionSpecificInstallDirectory)
				)
			)
			.CanClickNext(false);
	}
}
