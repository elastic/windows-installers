using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Location
{
	public class LocationModelTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public LocationModelTests()
		{
			this._model = WithValidPreflightChecks()
				.IsValidOnFirstStep();
		}

		[Fact] void InstallDirectoryNotEmpty() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = null;
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(string.Format(LocationsModelValidator.DirectoryMustBeSpecified, LocationsModelValidator.Installation))
			)
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = string.Empty;
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(string.Format(LocationsModelValidator.DirectoryMustBeSpecified, LocationsModelValidator.Installation))
			)
			.CanClickNext(false);

		[Fact] void InstallDirectoryMustBeRooted() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = @"..\Elasticsearch";
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(string.Format(LocationsModelValidator.DirectoryMustNotBeRelative, LocationsModelValidator.Installation))
			)
			.CanClickNext(false);

		[Fact] void MustBeOnKnownDrive() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = @"Y:\Elasticsearch";
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(string.Format(LocationsModelValidator.DirectoryUsesUnknownDrive, LocationsModelValidator.Installation, "Y:\\"))
			)
			.CanClickNext(false);

		[Fact] void ValidLocation() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = @"C:\Elasticsearch";
			})
			.IsValidOnStep(m => m.LocationsModel)
			.CanClickNext()
			.ClickRefresh()
			.OnStep(m=>m.LocationsModel, step =>
			{
				step.InstallDir.Should().Be(step.DefaultProductVersionInstallationDirectory);
			});
			
	}
}
