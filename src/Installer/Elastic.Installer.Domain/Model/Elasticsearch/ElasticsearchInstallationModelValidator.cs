using System.Linq;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch
{
	public class ElasticsearchInstallationModelValidator : AbstractValidator<ElasticsearchInstallationModel>
	{
		public static readonly string AlreadyInstalled = TextResources.NoticeModelValidator_AlreadyInstalled;
		public static readonly string HigherVersionInstalled = TextResources.NoticeModelValidator_HigherVersionInstalled;
		public static readonly string ConfigDirectoryIsSpecifiedAndSubPathOfEsHome = TextResources.NoticeModelValidator_ConfigDirectoryIsSpecifiedAndSubPathOfEsHome;
		public static readonly string HasEsHomeVariableButNoPreviousInstallation = TextResources.NoticeModelValidator_HasEsHomeVariableButNoPreviousInstallation;
		public static readonly string JavaInstalled = TextResources.NoticeModelValidator_JavaInstalled;
		public static readonly string JavaMisconfigured = TextResources.NoticeModelValidator_JavaMisconfigured;
		public static readonly string BadElasticsearchYamlFile = TextResources.NoticeModelValidator_BadElasticsearchYamlFile;
		public static readonly string NotAllModelsAreValid = TextResources.InstallationModelValidator_NotAllModelsAreValid;

		public ElasticsearchInstallationModelValidator()
		{
			RuleFor(vm => vm.JavaInstalled).Must(b => b).WithMessage(JavaInstalled);

			RuleFor(vm => vm.JavaMisconfigured).Must(b => !b).WithMessage(JavaMisconfigured);

			RuleFor(vm => vm.BadElasticsearchYamlFile).Must(b => !b).WithMessage(BadElasticsearchYamlFile);
			
			RuleFor(vm => vm.ConfigDirectoryIsSpecifiedAndSubPathOfEsHome).Must(b => !b).WithMessage(ConfigDirectoryIsSpecifiedAndSubPathOfEsHome);

			RuleFor(vm => vm.HasEsHomeVariableButNoPreviousInstallation).Must(b => !b).WithMessage(HasEsHomeVariableButNoPreviousInstallation);

			RuleFor(vm => vm.SameVersionAlreadyInstalled)
				.Must(b => !b)
				.When(vm => !vm.UnInstalling && !vm.Installing)
				.WithMessage(AlreadyInstalled);

			RuleFor(vm => vm.HigherVersionAlreadyInstalled)
				.Must(b => !b)
				.When(vm => vm.Installing)
				.WithMessage(HigherVersionInstalled);

			RuleFor(vm => vm.Steps)
				.Must(steps => steps.Where(s => s != null).All(s => s.IsValid))
				.When(vm => 
					vm.Steps != null
					&& !vm.JavaInstalled
					&& !vm.JavaMisconfigured
					&& !vm.SameVersionAlreadyInstalled
					&& !vm.HigherVersionAlreadyInstalled
					&& !vm.BadElasticsearchYamlFile)
				.WithMessage(NotAllModelsAreValid);
		}
	}
}
