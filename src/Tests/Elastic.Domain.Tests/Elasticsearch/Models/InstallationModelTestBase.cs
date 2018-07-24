using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models
{
	public class InstallationModelTestBase
	{
		protected string VersionSpecificInstallDirectory => Path.Combine(LocationsModel.DefaultProductInstallationDirectory, TestSetupStateProvider.DefaultTestVersion);
		
		protected InstallationModelTester Pristine() => InstallationModelTester.New();

		protected InstallationModelTester DefaultValidModel() => InstallationModelTester.ValidPreflightChecks();

		protected InstallationModelTester DefaultValidModel(Func<TestSetupStateProvider, TestSetupStateProvider> selector)  =>
			InstallationModelTester.ValidPreflightChecks(selector);

		protected InstallationModelTester WithExistingElasticsearchYaml(string yamlContents)  =>
			InstallationModelTester.ValidPreflightChecks(s=>s
				.Wix(alreadyInstalled: true)
				.Elasticsearch(e=>e
					.EsHomeMachineVariable(LocationsModel.DefaultProgramFiles)
					.EsConfigMachineVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(f=>
				{
					f.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(yamlContents));
					return f;
				})
			);
		
		protected InstallationModelTester DefaultValidModelForTasks() => DefaultValidModelForTasks(s=>s);

		protected InstallationModelTester DefaultValidModelForTasks(Func<TestSetupStateProvider, TestSetupStateProvider> selector)  =>
			InstallationModelTester.ValidPreflightChecksForTasks(selector);
	}
}

