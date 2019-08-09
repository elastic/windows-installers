using System;
using System.IO;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration
{
	public class JavaConfigurationTests
	{
		private readonly string _machineVariable = @"C:\Java\Machine";
		private readonly string _userVariable = @"C:\Java\User";
		private readonly string _processVariable = @"C:\Java\Process";
		private readonly string _esHome = @"C:\Java\EsHome";

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

		[Fact] void MachineHomeIsSeen() => AssertJavaHome(_machineVariable, m=> m.JavaHomeMachineVariable(_machineVariable));

		[Fact] void UserHomeIsSeen()=> AssertJavaHome(_userVariable, m=> m.JavaHomeUserVariable(_userVariable));

		[Fact] void ProcessHomeIsSeen()=> AssertJavaHome(_processVariable, m=> m.JavaHomeProcessVariable(_processVariable));

		[Fact] void JavaFromEsHomeIsSeen() => AssertJavaHome(Path.Combine(_esHome, "jdk"), esStateSetup: m => m.EsHomeMachineVariable(_esHome));

		[Fact] void ProcessBeatsUser() => AssertJavaHome(_processVariable, m => m
			.JavaHomeProcessVariable(_processVariable)
			.JavaHomeUserVariable(_userVariable)
		);

		[Fact] void UserBeatsMachine() => AssertJavaHome(_userVariable, m => m
			.JavaHomeUserVariable(_userVariable)
			.JavaHomeMachineVariable(_machineVariable)
		);
		
		[Fact] void MachineBeatsEsHome() => AssertJavaHome(_machineVariable, 
			m => m.JavaHomeMachineVariable(_machineVariable),
            m => m.EsHomeMachineVariable(_esHome)
		);
	}
}
