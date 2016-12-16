using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models
{
	public class InstallationModelTestBase
	{
		protected InstallationModelTester Pristine() => InstallationModelTester.New();

		protected InstallationModelTester WithValidPreflightChecks() => 
			InstallationModelTester.ValidPreflightChecks();

		protected InstallationModelTester WithValidPreflightChecks(Func<TestSetupStateProvider, TestSetupStateProvider> selector)  =>
			InstallationModelTester.ValidPreflightChecks(selector);

		protected InstallationModelTester WithExistingElasticsearchYaml(string yamlContents)  =>
			InstallationModelTester.ValidPreflightChecks(s=>s
				.Elasticsearch(e=>e
					.HomeDirectoryEnvironmentVariable(LocationsModel.DefaultProgramFiles)
					.ConfigDirectoryEnvironmentVariable(LocationsModel.DefaultConfigDirectory)
				)
				.FileSystem(f=>
				{
					f.AddFile(Path.Combine(LocationsModel.DefaultConfigDirectory, "elasticsearch.yml"), new MockFileData(yamlContents));
					return f;
				})
			);
	}
}

