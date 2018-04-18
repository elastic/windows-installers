using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.InstallerHosts.Elasticsearch.Tasks.Install;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.Tasks.Install
{
	public class SetupXPackPasswordsTaskTests : InstallationModelTestBase
	{
		[Fact(Skip = "need an integration test for this")]
		void InstallByDefault() => WithValidPreflightChecks()
			.OnStep(m=>m.XPackModel, step =>
			{
				step.XPackLicense = XPackLicenseMode.Trial;

				step.ElasticUserPassword = "somepass";
				step.KibanaUserPassword = "somepass";
				step.LogstashSystemUserPassword = "somepass";

			})
			.AssertTask(
				(m, s, fs) => new SetupXPackPasswordsTask(m, s, fs), 
				(m, t) =>
				{
				}
			);
	}
}
