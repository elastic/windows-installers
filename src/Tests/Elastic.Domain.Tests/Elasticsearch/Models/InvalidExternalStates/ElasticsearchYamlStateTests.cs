using Elastic.Installer.Domain.Properties;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.InvalidExternalStates
{
	public class ElasticsearchYamlStateTests : InstallationModelTestBase
	{

		[Fact] void SeedsFromEmptyElasticsearchYamlWithEnters() => WithExistingElasticsearchYaml($@"
"
				)
				.IsValidOnFirstStep();

		[Fact] void SeedsFromEmptyElasticsearchYaml() => WithExistingElasticsearchYaml($@"")
				.IsValidOnFirstStep();
		
		[Fact] void SeedsInvalidYamlFile() => 
			WithExistingElasticsearchYaml($@"x=")
				.HasSetupValidationFailures(errors=>errors
					.ShouldHaveErrors(
						TextResources.NoticeModelValidator_BadElasticsearchYamlFile
					)
				);

	}
}
