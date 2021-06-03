using System;
using System.IO;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration
{
	public class EsJavaHomeConfigurationTests
	{
		private readonly string _machineVariable = @"C:\LegacyJavaHome\Machine";
		private readonly string _userVariable = @"C:\LegacyJavaHome\User";
		private readonly string _processVariable = @"C:\LegacyJavaHome\Process";
		private readonly string _esHome = @"C:\LegacyJavaHome\EsHome";

		private void AssertJavaHome(string expect,
			Func<MockJavaEnvironmentStateProvider, IJavaEnvironmentStateProvider> javaStateSetup = null,
			Func<MockElasticsearchEnvironmentStateProvider, IElasticsearchEnvironmentStateProvider> esStateSetup = null)
		{
			var javaConfiguration = new JavaConfiguration(
				javaStateSetup != null
					? javaStateSetup(new MockJavaEnvironmentStateProvider())
					: new MockJavaEnvironmentStateProvider(),
				esStateSetup != null
					? esStateSetup(new MockElasticsearchEnvironmentStateProvider())
					: new MockElasticsearchEnvironmentStateProvider());

			javaConfiguration.JavaHomeCanonical.Should().Be(expect);
		}

		[Fact] void MachineHomeIsSeen() => AssertJavaHome(_machineVariable,
			m => m.EsJavaHomeMachineVariable(_machineVariable));

		[Fact] void UserHomeIsSeen() => AssertJavaHome(_userVariable,
			m => m.EsJavaHomeUserVariable(_userVariable));

		[Fact] void ProcessHomeIsSeen() => AssertJavaHome(_processVariable, 
			m => m.EsJavaHomeProcessVariable(_processVariable));

		[Fact] void JavaFromEsHomeIsSeen() => AssertJavaHome(Path.Combine(_esHome, "jdk"), 
			esStateSetup: m => m.EsHomeMachineVariable(_esHome));

		[Fact]
		void ProcessBeatsUser() => AssertJavaHome(_processVariable, m => m
			.EsJavaHomeProcessVariable(_processVariable)
			.EsJavaHomeUserVariable(_userVariable)
		);

		[Fact]
		void UserBeatsMachine() => AssertJavaHome(_userVariable, m => m
			.EsJavaHomeUserVariable(_userVariable)
			.EsJavaHomeMachineVariable(_machineVariable)
		);

		[Fact]
		void MachineBeatsEsHome() => AssertJavaHome(_machineVariable,
			m => m.EsJavaHomeMachineVariable(_machineVariable),
			m => m.EsHomeMachineVariable(_esHome)
		);
	}

	public class LegacyJavaHomeConfigurationTests
	{
		private readonly string _machineVariable = @"C:\EsJavaHome\Machine";
		private readonly string _userVariable = @"C:\EsJavaHome\User";
		private readonly string _processVariable = @"C:\EsJavaHome\Process";
		private readonly string _esHome = @"C:\EsJavaHome\EsHome";

		private void AssertJavaHome(string expect,
			Func<MockJavaEnvironmentStateProvider, IJavaEnvironmentStateProvider> javaStateSetup = null,
			Func<MockElasticsearchEnvironmentStateProvider, IElasticsearchEnvironmentStateProvider> esStateSetup = null)
		{
			var javaConfiguration = new JavaConfiguration(
				javaStateSetup != null
					? javaStateSetup(new MockJavaEnvironmentStateProvider())
					: new MockJavaEnvironmentStateProvider(),
				esStateSetup != null
					? esStateSetup(new MockElasticsearchEnvironmentStateProvider())
					: new MockElasticsearchEnvironmentStateProvider());

			javaConfiguration.JavaHomeCanonical.Should().Be(expect);
		}

		[Fact]
		void MachineHomeIsSeen() => AssertJavaHome(_machineVariable,
			m => m.LegacyJavaHomeMachineVariable(_machineVariable));

		[Fact]
		void UserHomeIsSeen() => AssertJavaHome(_userVariable,
			m => m.LegacyJavaHomeUserVariable(_userVariable));

		[Fact]
		void ProcessHomeIsSeen() => AssertJavaHome(_processVariable,
			m => m.LegacyJavaHomeProcessVariable(_processVariable));

		[Fact]
		void JavaFromEsHomeIsSeen() => AssertJavaHome(Path.Combine(_esHome, "jdk"),
			esStateSetup: m => m.EsHomeMachineVariable(_esHome));

		[Fact]
		void ProcessBeatsUser() => AssertJavaHome(_processVariable, m => m
			.LegacyJavaHomeProcessVariable(_processVariable)
			.LegacyJavaHomeUserVariable(_userVariable)
		);

		[Fact]
		void UserBeatsMachine() => AssertJavaHome(_userVariable, m => m
			.LegacyJavaHomeUserVariable(_userVariable)
			.LegacyJavaHomeMachineVariable(_machineVariable)
		);

		[Fact]
		void MachineBeatsEsHome() => AssertJavaHome(_machineVariable,
			m => m.LegacyJavaHomeMachineVariable(_machineVariable),
			m => m.EsHomeMachineVariable(_esHome)
		);
	}
}
