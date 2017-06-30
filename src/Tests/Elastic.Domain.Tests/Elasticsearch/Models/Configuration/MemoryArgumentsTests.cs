using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Configuration
{
	public class MemoryArgumentsTests : InstallationModelArgumentsTestsBase
	{
		[Fact] void SelectedMemoryTooLittle() => Argument(nameof(ConfigurationModel.SelectedMemory), 1, (m, v) =>
		{
			m.ConfigurationModel.SelectedMemory.Should().Be((ulong)v);
			m.ConfigurationModel.IsValid.Should().BeFalse();
			m.ConfigurationModel.LockMemory.Should().BeFalse();
		});

		[Fact] void SelectedMemory() => Argument(nameof(ConfigurationModel.SelectedMemory), ConfigurationModel.DefaultHeapSize, (m, v) =>
		{
			m.ConfigurationModel.SelectedMemory.Should().Be(v);
			m.ConfigurationModel.IsValid.Should().BeTrue();
		});

		[Fact] void LockMemory() => Argument(nameof(ConfigurationModel.LockMemory), "TRuE", "true", (m, v) =>
		{
			m.ConfigurationModel.LockMemory.Should().BeTrue();
		});

		[Fact] void LockMemoryFalse() => Argument(nameof(ConfigurationModel.LockMemory), "FalSE", "false", (m, v) =>
		{
			m.ConfigurationModel.LockMemory.Should().BeFalse();
		});


	}
}
