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
			)));

		[Fact] public void DefaultStateNeedsJava() => EmptyJavaModel(s=>s)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaInstalled)
			);

		[Fact] public void JavaUserEnvironmentVariable() => EmptyJavaModel(s=>s
				.Java(j => j.JavaHomeUserVariable(@"C:\Java"))
			)
			.IsValidOnFirstStep();

		[Fact] public void JavaHomeFromEsHome() => EmptyJavaModel(s=>s
				.Elasticsearch(e => e.EsHomeMachineVariable(@"C:\Java"))
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
	}
}
