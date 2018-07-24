using Elastic.Installer.Domain.Properties;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.InvalidExternalStates
{
	public class WixStateTests : InstallationModelTestBase
	{
		private InstallationModelTester WixModel(string currentVersion, string previousVersion) => 
			DefaultValidModel(s => s.Wix(currentVersion, previousVersion));

		private InstallationModelTester WixModel(bool alreadyInstalled) => DefaultValidModel(s => s.Wix(alreadyInstalled));

		[Fact] public void PatchUpgrade() => WixModel("5.0.1", "5.0.0")
			.IsValidOnFirstStep();


	}
}
