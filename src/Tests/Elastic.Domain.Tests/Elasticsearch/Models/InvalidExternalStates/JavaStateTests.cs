using System;
using Elastic.Installer.Domain.Properties;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.InvalidExternalStates
{
	public class JavaStateTests : InstallationModelTestBase
	{
		private InstallationModelTester EmptyJavaModel(
			Func<TestSetupStateProvider, TestSetupStateProvider> selector) => DefaultValidModel(s => selector(s
				.Java(j => j
					.EsJavaHomeMachineVariable(null)
					.EsJavaHomeUserVariable(null)
					.LegacyJavaHomeMachineVariable(null)
					.LegacyJavaHomeUserVariable(null)
				)));

		[Fact]
		public void DefaultStateNeedsJava() => EmptyJavaModel(s => s)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaInstalled)
			);


		[Fact]
		public void EsJavaHomeUserEnvironmentVariable() => EmptyJavaModel(s => s
		 .Java(j => j.EsJavaHomeUserVariable(@"C:\EsJavaHome"))
			)
			.IsValidOnFirstStep();


		[Fact]
		public void EsJavaHomeMachineVariable() => EmptyJavaModel(s => s
		 .Java(j => j.EsJavaHomeMachineVariable(@"C:\EsJavaHome"))
			)
			.IsValidOnFirstStep();

		[Fact]
		public void EsJavaHomeMachineAndUserVariableSame() => EmptyJavaModel(s => s
		 .Java(j => j.EsJavaHomeMachineVariable(@"C:\EsJavaHome").EsJavaHomeUserVariable(@"C:\EsJavaHome"))
			)
			.IsValidOnFirstStep();

		[Fact]
		public void EsJavaHomeMachineAndUserVariableNOTSame() => EmptyJavaModel(s => s
		 .Java(j => j.EsJavaHomeMachineVariable(@"C:\EsJavaHome").EsJavaHomeUserVariable(@"C:\EsJavaHomeX"))
			)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaMisconfigured)
			);


		[Fact]
		public void LegacyJavaHomeUserEnvironmentVariable() => EmptyJavaModel(s => s
		 .Java(j => j.LegacyJavaHomeUserVariable(@"C:\LegacyJavaHome"))
			)
			.IsValidOnFirstStep();

		[Fact]
		public void LegacyJavaHomeMachineVariable() => EmptyJavaModel(s => s
		 .Java(j => j.LegacyJavaHomeMachineVariable(@"C:\LegacyJavaHome"))
			)
			.IsValidOnFirstStep();

		[Fact]
		public void LegacyJavaHomeMachineAndUserVariableSame() => EmptyJavaModel(s => s
		 .Java(j => j.LegacyJavaHomeMachineVariable(@"C:\LegacyJavaHome").LegacyJavaHomeUserVariable(@"C:\LegacyJavaHome"))
			)
			.IsValidOnFirstStep();

		[Fact]
		public void LegacyJavaHomeMachineAndUserVariableNOTSame() => EmptyJavaModel(s => s
		 .Java(j => j.LegacyJavaHomeMachineVariable(@"C:\LegacyJavaHome").LegacyJavaHomeUserVariable(@"C:\LegacyJavaHomeX"))
			)
			.HasPrerequisiteErrors(errors => errors
				.ShouldHaveErrors(TextResources.NoticeModelValidator_JavaMisconfigured)
			);


		[Fact]
		public void JavaHomeFromEsHome() => EmptyJavaModel(s => s
		 .Elasticsearch(e => e.EsHomeMachineVariable(@"C:\Java"))
			)
			.IsValidOnFirstStep();
	}
}
