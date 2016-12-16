using System;
using FluentAssertions;
using ReactiveUI;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Location
{
	/*
		LocationsModel has two properties that dictate how to handle set directories

		- ConfigureLocations, wheter the user wants to role with the default configurations or not. 
		  In the UI this is presented as a radio button but we need to assert that manually setting the installation directory 
		  updates this value too. We do not want to have to expose this as a required flag in the silent install

		- ConfigureAllLocations, whether the user wants the manually set the writable folders (config, data, log)
		  this value is computed and setting any of the writable folders in the viewmodel should toggle this to true
	*/
	public class LocationsFlagTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;

		public LocationsFlagTests()
		{
			this._model = WithValidPreflightChecks()
				.IsValidOnFirstStep();
		}

		[Fact] void ConfigureLocationsShouldBeFalse() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigureLocations.Should().BeFalse();
			});

		[Fact] void ConfigureAllLocationsShouldBeFalse() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigureAllLocations.Should().BeFalse();
			});

		[Fact] void ConfigureAllIsReactive() => this._model 
			//When setting configure locations ConfigureAllLocations is also set
			.OnStep(m => m.LocationsModel, step =>
			{
				var allLocations = false;
				step.WhenAnyValue(vm => vm.ConfigureAllLocations).Subscribe(b => allLocations = b);
				step.ConfigureLocations = true;
				allLocations.Should().BeTrue();
			})
			//When setting PlaceWritableLocationsInSamePath however we are no longer configuring all locations
			.OnStep(m => m.LocationsModel, step =>
			{
				var allLocations = true;
				step.WhenAnyValue(vm => vm.ConfigureAllLocations).Subscribe(b => allLocations = b);
				step.PlaceWritableLocationsInSamePath = true;
				allLocations.Should().BeFalse();
			});

		[Fact] void UpdatingInstallDirectoryShouldSetConfigureLocations() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.InstallDir = "C:\\Elasticsearch";
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
			});

		[Fact] void UpdatingDataDirectoryShouldBothFlags() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.DataDirectory = "C:\\Elasticsearch";
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
			});

		[Fact] void UpdatingConfDirectoryShouldBothFlags() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigDirectory = "C:\\Elasticsearch";
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
			});

		[Fact] void UpdatingLogsDirectoryShouldBothFlags() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.LogsDirectory = "C:\\Elasticsearch";
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeTrue();
			})
			.OnStep(m => m.LocationsModel, step =>
			{
				step.PlaceWritableLocationsInSamePath = true;
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeFalse();
				step.LogsDirectory.Should().NotBe("C:\\Elasticsearch");
			});

		[Fact] void UnsettingConfigureLocationsResets() => this._model
			.OnStep(m => m.LocationsModel, step =>
			{
				step.LogsDirectory = "C:\\Elasticsearch";
				step.PlaceWritableLocationsInSamePath = true;
				step.ConfigureLocations.Should().BeTrue();
				step.ConfigureAllLocations.Should().BeFalse();
			})
			.OnStep(m => m.LocationsModel, step =>
			{
				step.ConfigureLocations = false;
				step.LogsDirectory.Should().NotBe("C:\\Elasticsearch");
				step.ConfigureAllLocations.Should().BeFalse();
				step.ConfigureLocations.Should().BeFalse();
				step.PlaceWritableLocationsInSamePath.Should().BeFalse();
			});

	}
}
