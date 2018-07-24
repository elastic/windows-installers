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
		
		private new string VersionSpecificInstallDirectory => Path.Combine(this._installDirectory, TestSetupStateProvider.DefaultTestVersion);

		public WritableLocationsTests()
		{
			this._model = DefaultValidModel()
				.IsValidOnStep(m => m.LocationsModel);
		}

		[Fact] void LogsDirectoryCanNotBeChildOfInstallDirectory() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = _installDirectory;
			})
			.IsValidOnStep(m=>m.LocationsModel)
			.OnStep(m => m.LocationsModel, step =>
			{
				step.LogsDirectory = Path.Combine(step.InstallDir, "logs");
				step.DataDirectory = Path.Combine(step.InstallDir, "data");
				step.ConfigDirectory = Path.Combine(step.InstallDir, "conf");
			})
			.IsInvalidOnStep(m => m.LocationsModel, errors => errors
				.ShouldHaveErrors(
					string.Format(LocationsModelValidator.DirectoryMustNotBeChildOf, LocationsModelValidator.LogsText),
					string.Format(LocationsModelValidator.DirectoryMustNotBeChildOf, LocationsModelValidator.DataText),
					string.Format(LocationsModelValidator.DirectoryMustNotBeChildOf, LocationsModelValidator.ConfigurationText)
				)
			)
			.CanClickNext(false);
	}
}
