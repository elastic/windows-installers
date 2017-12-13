using System.IO;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Location
{
	public class LocationModelTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;
		private const string CustomInstallationFolder = @"C:\Elasticsearch";

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
				step.PreviousInstallationDirectory.Should().BeNullOrEmpty();
			});
		
		[Fact] void ValidLocationUsingPreviousVersion6X() => WithValidPreflightChecks(s=>s.Wix("6.1.0", "6.0.0"))
			.IsValidOnFirstStep()
            .OnStep(m => m.LocationsModel, step =>
            {
	            step.InstallDir = CustomInstallationFolder;
            })
            .CanClickNext()
            .OnStep(m=>m.LocationsModel, step =>
            {
                step.InstallDir.Should().Be(Path.Combine(CustomInstallationFolder, "6.1.0"));
                step.PreviousInstallationDirectory.Should().Be(Path.Combine(CustomInstallationFolder, "6.0.0"));
            });
		
		[Fact] void ValidLocationUsingPreviousVersion5X() => WithValidPreflightChecks(s=>s.Wix("6.0.0", "5.0.0", CustomInstallationFolder))
			.IsValidOnFirstStep()
            .OnStep(m => m.LocationsModel, step =>
            {
	            step.InstallDir = CustomInstallationFolder;
            })
            .CanClickNext()
            .OnStep(m=>m.LocationsModel, step =>
            {
                step.InstallDir.Should().Be(Path.Combine(CustomInstallationFolder, "6.0.0"));
                step.PreviousInstallationDirectory.Should().Be(Path.Combine(CustomInstallationFolder));
            });		
	}
}
