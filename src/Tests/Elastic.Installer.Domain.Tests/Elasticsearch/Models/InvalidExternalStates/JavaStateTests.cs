using System;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.InvalidExternalStates
{
	public class JavaStateTests : InstallationModelTestBase
	{
		private InstallationModelTester EmptyJavaModel(Func<TestSetupStateProvider, TestSetupStateProvider> selector) => WithValidPreflightChecks(s => selector(s
			.Java(j => j
				.JavaHomeMachine(null)
				.JavaHomeCurrentUser(null)
				.JavaHomeRegistry(null)
			)));

		[Fact] public void DefaultStateNeedsJava() => EmptyJavaModel(s=>s)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaInstalled)
			);

		/// <summary>
		/// User home will be lifted to a machine level environment variable during install
		/// </summary>
		[Fact] public void JavaUserEnvironmentVariable() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeCurrentUser(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		/// <summary>
		/// Registry java home will be set as JAVA_HOME later 
		/// </summary>
		[Fact] public void JavaHomeOnlyInRegistry() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeRegistry(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		[Fact] public void JavaHomeMachineVariable() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachine(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		[Fact] public void MachineAndUserVariableSame() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachine(@"C:\Java").JavaHomeCurrentUser(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		[Fact] public void MachineAndUserVariableNOTSame() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachine(@"C:\Java").JavaHomeCurrentUser(@"C:\JavaX"))
			)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaMisconfigured)
			);

		[Fact] public void MachineAndRegistryNOTSame() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachine(@"C:\Java").JavaHomeRegistry(@"C:\JavaX"))
			)
			.IsValidOnFirstStep();

	}
}
