using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class MemoryTests : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;
		private readonly ulong _smallHeapSize = ConfigurationModel.DefaultHeapSize / 2;

		public MemoryTests()
		{
			this._model = DefaultValidModel()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.ConfigurationModel);
		}

		[Fact] void MemoryTooLow() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.SelectedMemory = 1; })
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				ConfigurationModelValidator.SelectedMemoryGreaterThanOrEqual250Mb
			))
			.CanClickNext(false);

		//[Fact] void MemoryTooHigh() => this._model
		//	.OnStep(m => m.ConfigurationModel, step => { step.SelectedMemory = (step.TotalPhysicalMemory / 2) + 1; })
		//	.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
		//		ConfigurationModelValidator.MaxMemoryUpTo50Percent
		//	))
		//	.CanClickNext(false);

		[Fact] void MinimumSelectedMemoryGreaterThanZero() => this._model
			.OnStep(m => m.ConfigurationModel, step => { step.MinSelectedMemory.Should().BeGreaterThan(0); });
			
		[Fact] void MaxSelectedMemoryIsHalfAvailableAndReactive() => this._model
			.OnStep(m => m.ConfigurationModel, step => 
			{
				if (ConfigurationModel.DefaultTotalPhysicalMemory < ConfigurationModel.CompressedOrdinaryPointersThreshold * 2)
				{
					step.MaxSelectedMemory.Should().Be(ConfigurationModel.DefaultTotalPhysicalMemory / 2);
					step.TotalPhysicalMemory = ConfigurationModel.DefaultTotalPhysicalMemory / 2;
					step.MaxSelectedMemory.Should().Be(ConfigurationModel.DefaultTotalPhysicalMemory / 4);
				}
			});

		[Fact] void DefaultMaxDoesNotExceedCompressedOrdinaryPointersThreshold() => this._model
			.OnStep(m => m.ConfigurationModel, step => 
			{
				step.TotalPhysicalMemory = ConfigurationModel.CompressedOrdinaryPointersThreshold * 4;
				step.MaxSelectedMemory.Should().NotBe(ConfigurationModel.DefaultTotalPhysicalMemory / 2);
				step.MaxSelectedMemory.Should().Be(ConfigurationModel.CompressedOrdinaryPointersThreshold);
			});

		[Fact] void CanNotSelectOverCompressedOrdinaryPointersThreshold() => this._model
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.TotalPhysicalMemory = ConfigurationModel.CompressedOrdinaryPointersThreshold * 4;
				step.SelectedMemory = step.TotalPhysicalMemory / 2;
			})
			.IsInvalidOnStep(m => m.ConfigurationModel, errors => errors.ShouldHaveErrors(
				ConfigurationModelValidator.SelectedMemoryLessThan32Gb
			));


		[Fact] void RespectsJvmOptsFile() => DefaultValidModel(s => s
				.Wix(alreadyInstalled: true)
				.Elasticsearch(e => e
					.EsHomeMachineVariable(LocationsModel.DefaultProgramFiles)
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(f =>
				{
					f.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "jvm.options"), new MockFileData($@"-Xmx{_smallHeapSize}m"));
					return f;
				})
			)
			.OnStep(m => m.ConfigurationModel, step =>
			{
				step.SelectedMemory.Should().Be(_smallHeapSize);
			})
			.IsValidOnFirstStep();
	}
}
