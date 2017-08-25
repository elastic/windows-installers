using System.Linq;
using System.Management.Instrumentation;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch
{
	public class ElasticsearchInstallationModelValidator : AbstractValidator<ElasticsearchInstallationModel>
	{
		public static readonly string AlreadyInstalled = TextResources.NoticeModelValidator_AlreadyInstalled;
		public static readonly string HigherVersionInstalled = TextResources.NoticeModelValidator_HigherVersionInstalled;
		public static readonly string JavaInstalled = TextResources.NoticeModelValidator_JavaInstalled;
		public static readonly string JavaMisconfigured = TextResources.NoticeModelValidator_JavaMisconfigured;
		public static readonly string Using32BitJava = TextResources.NoticeModelValidator_Using32BitJava;
		public static readonly string BadElasticsearchYamlFile = TextResources.NoticeModelValidator_BadElasticsearchYamlFile;
		public static readonly string NotAllModelsAreValid = TextResources.InstallationModelValidator_NotAllModelsAreValid;

		public ElasticsearchInstallationModelValidator()
		{
			RuleFor(vm => vm.JavaInstalled).Must(b => b).WithMessage(JavaInstalled);

			RuleFor(vm => vm.JavaMisconfigured).Must(b => !b).WithMessage(JavaMisconfigured);

			RuleFor(vm => vm.Using32BitJava).Must(b => !b).WithMessage(Using32BitJava);
			
			RuleFor(vm => vm.BadElasticsearchYamlFile).Must(b => !b).WithMessage(BadElasticsearchYamlFile);

			RuleFor(vm => vm.SameVersionAlreadyInstalled)
				.Must(b => !b)
				.When(vm => !vm.UnInstalling && !vm.Installing)
				.WithMessage(AlreadyInstalled);

			RuleFor(vm => vm.HigherVersionAlreadyInstalled).Must(b => !b).WithMessage(HigherVersionInstalled);

			RuleFor(vm => vm.Steps)
				.Must(steps => steps.Where(s => s != null).All(s => s.IsValid))
				.When(vm => !vm.JavaInstalled
							&& !vm.JavaMisconfigured
							&& !vm.Using32BitJava
							&& !vm.SameVersionAlreadyInstalled
							&& !vm.HigherVersionAlreadyInstalled
							&& !vm.BadElasticsearchYamlFile)
				.WithMessage(NotAllModelsAreValid);
		}
	}
}
