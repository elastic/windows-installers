using System;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.InvalidExternalStates
{
	public class JavaStateTests : InstallationModelTestBase
	{
		private InstallationModelTester EmptyJavaModel(Func<TestSetupStateProvider, TestSetupStateProvider> selector) => DefaultValidModel(s => selector(s
			.Java(j => j
				.JavaHomeMachineVariable(null)
				.JavaHomeUserVariable(null)
				.JdkRegistry64(null)
			)));

		[Fact] public void DefaultStateNeedsJava() => EmptyJavaModel(s=>s)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaInstalled)
			);

		/// <summary>
		/// User home will be lifted to a machine level environment variable during install
		/// </summary>
		[Fact] public void JavaUserEnvironmentVariable() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeUserVariable(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		/// <summary>
		/// Registry java home will be set as JAVA_HOME later 
		/// </summary>
		[Fact] public void JavaHomeOnlyInRegistry() => EmptyJavaModel(s=>s
				.Java(j => j.JdkRegistry64(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		[Fact] public void JavaHomeMachineVariable() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachineVariable(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		[Fact] public void MachineAndUserVariableSame() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachineVariable(@"C:\Java").JavaHomeUserVariable(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		[Fact] public void MachineAndUserVariableNOTSame() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachineVariable(@"C:\Java").JavaHomeUserVariable(@"C:\JavaX"))
			)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaMisconfigured)
			);

		[Fact] public void MachineAndRegistryNOTSame() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeMachineVariable(@"C:\Java").JdkRegistry64(@"C:\JavaX"))
			)
			.IsValidOnFirstStep();
		
		[Fact] public void Jdk32BitsOnlyIsAPrequisiteFailure() => EmptyJavaModel(s=>s
				.Java(j => j.JdkRegistry32(@"C:\Java32"))
			)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_Using32BitJava)
			);
		
		[Fact] public void Jre32BitsOnlyIsAPrequisiteFailure() => EmptyJavaModel(s=>s
				.Java(j => j.JreRegistry32(@"C:\Java32"))
			)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_Using32BitJava)
			);
		
		[Fact] public void Jre32And64IsValid() => EmptyJavaModel(s=>s
				.Java(j => j.JreRegistry64(@"C:\Java").JreRegistry32(@"c:\Java32"))
			)
			.IsValidOnFirstStep();
		
		[Fact] public void Jdk32And64IsValid() => EmptyJavaModel(s=>s
				.Java(j => j.JdkRegistry64(@"C:\Java").JdkRegistry32(@"c:\Java32"))
			)
			.IsValidOnFirstStep();

	}
}
