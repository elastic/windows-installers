using System;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentAssertions;
using Xunit;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models.XPack
{
	public class TrialLicenseModelTester : InstallationModelTestBase
	{
		private readonly InstallationModelTester _model;
		
		public TrialLicenseModelTester()
		{
			this._model = WithValidPreflightChecks()
				.ClickNext()
				.ClickNext()
				.ClickNext()
				.IsValidOnStep(m => m.PluginsModel)
				.IsValidOnStep(m => m.PluginsModel)
				.ClickNext()
				.OnStep(m => m.XPackModel, step =>
				{
					step.XPackLicense = XPackLicenseMode.Trial;
				})
				.IsInvalidOnStep(m => m.XPackModel,errors => errors
					.ShouldHaveErrors(
						XPackModelValidator.ElasticPasswordRequired,
						XPackModelValidator.KibanaPasswordRequired,
						XPackModelValidator.LogstashPasswordRequired
					)
				);
		}

		[Fact] void SettingShortPasswordsStillAnError() => this._model
			.OnStep(m => m.XPackModel, step => 
			{
				step.XPackLicense.Should().Be(XPackLicenseMode.Trial);
				step.ElasticUserPassword = Guid.NewGuid().ToString().Substring(0, 4);
				step.KibanaUserPassword = Guid.NewGuid().ToString().Substring(0, 4);
				step.LogstashSystemUserPassword = Guid.NewGuid().ToString().Substring(0, 4);
			})
			.IsInvalidOnStep(m => m.XPackModel,errors => errors
				.ShouldHaveErrors(
					XPackModelValidator.ElasticPasswordAtLeast6Characters,
					XPackModelValidator.KibanaPasswordAtLeast6Characters,
					XPackModelValidator.LogstashPasswordAtLeast6Characters
				)
			)
			.CanClickNext(canClick: false);
		
		[Fact] void SettingPasswordsMakesTheModelValid() => this._model
			.OnStep(m => m.XPackModel, step => 
			{
				step.XPackLicense.Should().Be(XPackLicenseMode.Trial);
				step.ElasticUserPassword = Guid.NewGuid().ToString().Substring(0, 6);
				step.KibanaUserPassword = Guid.NewGuid().ToString().Substring(0, 6);
				step.LogstashSystemUserPassword = Guid.NewGuid().ToString().Substring(0, 6);
			})
			.IsValidOnStep(m => m.XPackModel)
			.CanClickNext();
		
		[Fact] void CanForceManualPasswordGeneration() => this._model
			.OnStep(m => m.XPackModel, step => 
			{
				step.XPackLicense.Should().Be(XPackLicenseMode.Trial);
				step.SkipSettingPasswords = true;
			})
			.IsValidOnStep(m => m.XPackModel)
			.CanClickNext();
		
		[Fact] void CanDisableXPackSecurity() => this._model
			.OnStep(m => m.XPackModel, step => 
			{
				step.XPackLicense.Should().Be(XPackLicenseMode.Trial);
				step.XPackSecurityEnabled.Should().BeTrue();
				step.XPackSecurityEnabled = false;
			})
			.IsValidOnStep(m => m.XPackModel)
			.CanClickNext();
		
		[Fact] void UpdatingServiceToNotInstallShouldNoLongerAskForPasswords() => this._model
			.ClickBack()
			.ClickBack()
			.ClickBack()
			.IsOnStep(m => m.ServiceModel)
			.OnStep(m => m.ServiceModel, step =>
			{
				step.InstallAsService = false;
			})
			.ClickNext()
			.ClickNext()
			.ClickNext()
			.OnStep(m => m.XPackModel, step => 
			{
				step.XPackLicense.Should().Be(XPackLicenseMode.Trial);
				step.SkipSettingPasswords.Should().BeTrue();
			})
			.IsValidOnStep(m => m.XPackModel)
			.CanInstall();
		
		[Fact] void UpdatingServiceToNotStartAfterInstallShouldNoLongerAskForPasswords() => this._model
			.ClickBack()
			.ClickBack()
			.ClickBack()
			.IsOnStep(m => m.ServiceModel)
			.OnStep(m => m.ServiceModel, step =>
			{
				step.StartAfterInstall = false;
			})
			.ClickNext()
			.ClickNext()
			.ClickNext()
			.OnStep(m => m.XPackModel, step => 
			{
				step.XPackLicense.Should().Be(XPackLicenseMode.Trial);
				step.SkipSettingPasswords.Should().BeTrue();
			})
			.IsValidOnStep(m => m.XPackModel)
			.CanInstall();
		
			
	}
}
