using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Location
{
	public class WritableLocationsTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;
		private string _installDirectory = "C:\\elasticsearch";

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
					.And.StartWith(LocationsModel.DefaultInstallationDirectory);
				step.ConfigDirectory.Should().StartWith(step.InstallDir)
					.And.StartWith(LocationsModel.DefaultInstallationDirectory);
				step.DataDirectory.Should().StartWith(step.InstallDir)
					.And.StartWith(LocationsModel.DefaultInstallationDirectory);;
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
					string.Format(LocationsModelValidator.DirectoryMustBeChildOf, LocationsModelValidator.LogsText, _installDirectory),
					string.Format(LocationsModelValidator.DirectoryMustBeChildOf, LocationsModelValidator.DataText, _installDirectory),
					string.Format(LocationsModelValidator.DirectoryMustBeChildOf, LocationsModelValidator.ConfigurationText, _installDirectory)
				)
			)
			.CanClickNext(false);
	}
}
