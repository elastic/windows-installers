using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration
{
	public class JavaConfigurationTests
	{
		private readonly string _defaultJavaDirectory = @"C:\Java";


		[Fact] void EmptyJavaStateShouldNotWriteJavaHome()
		{
			var javaConfiguration = new JavaConfiguration(new MockJavaEnvironmentStateProvider());
			string javaHome;
			var hasJavaHome = javaConfiguration.SetJavaHome(out javaHome);
			javaHome.Should().BeNullOrEmpty();
			hasJavaHome.Should().BeFalse();
		}
		
		[Fact] void MachineHomeIsSeen()
		{
			var javaConfiguration = new JavaConfiguration(new MockJavaEnvironmentStateProvider()
				.JavaHomeMachine(_defaultJavaDirectory)
				);
			string javaHome;
			var hasJavaHome = javaConfiguration.SetJavaHome(out javaHome);
			javaHome.Should().Be(_defaultJavaDirectory);
			hasJavaHome.Should().BeTrue();
		}

		[Fact] void CurrentUserHomeIsSeen()
		{
			var javaConfiguration = new JavaConfiguration(new MockJavaEnvironmentStateProvider()
				.JavaHomeCurrentUser(_defaultJavaDirectory)
				);
			string javaHome;
			var hasJavaHome = javaConfiguration.SetJavaHome(out javaHome);
			javaHome.Should().Be(_defaultJavaDirectory);
			hasJavaHome.Should().BeTrue();
		}

		[Fact] void RegistryHomeIsSeen()
		{
			var javaConfiguration = new JavaConfiguration(new MockJavaEnvironmentStateProvider()
				.JavaHomeRegistry(_defaultJavaDirectory)
				);
			string javaHome;
			var hasJavaHome = javaConfiguration.SetJavaHome(out javaHome);
			javaHome.Should().Be(_defaultJavaDirectory);
			hasJavaHome.Should().BeTrue();
		}

		[Fact] void ShouldNotOverwriteMachineLevelJavaIfUserIsSet()
		{
			var javaConfiguration = new JavaConfiguration(new MockJavaEnvironmentStateProvider()
				.JavaHomeMachine(_defaultJavaDirectory).JavaHomeCurrentUser(_defaultJavaDirectory + "X")
				);
			string javaHome;
			var hasJavaHome = javaConfiguration.SetJavaHome(out javaHome);
			javaHome.Should().Be(_defaultJavaDirectory);
			hasJavaHome.Should().BeTrue();
		}

		[Fact] void ShouldNotOverwriteMachineLevelJavaIfRegistryIsSet()
		{
			var javaConfiguration = new JavaConfiguration(new MockJavaEnvironmentStateProvider()
				.JavaHomeMachine(_defaultJavaDirectory).JavaHomeRegistry(_defaultJavaDirectory + "X")
				);
			string javaHome;
			var hasJavaHome = javaConfiguration.SetJavaHome(out javaHome);
			javaHome.Should().Be(_defaultJavaDirectory);
			hasJavaHome.Should().BeTrue();
		}

		[Fact] void CurrentUserWinsFromRegistry()
		{
			var javaConfiguration = new JavaConfiguration(new MockJavaEnvironmentStateProvider()
				.JavaHomeCurrentUser(_defaultJavaDirectory).JavaHomeRegistry(_defaultJavaDirectory + "X")
				);
			string javaHome;
			var hasJavaHome = javaConfiguration.SetJavaHome(out javaHome);
			javaHome.Should().Be(_defaultJavaDirectory);
			hasJavaHome.Should().BeTrue();
		}
	}
}
