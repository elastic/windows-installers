using System.Linq;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Elasticsearch.Model
{
	public class InstallationModelValidator : AbstractValidator<InstallationModel>
	{
		public static readonly string AlreadyInstalled = TextResources.NoticeModelValidator_AlreadyInstalled;
		public static readonly string HigherVersionInstalled = TextResources.NoticeModelValidator_HigherVersionInstalled;
		public static readonly string JavaInstalled = TextResources.NoticeModelValidator_JavaInstalled;
		public static readonly string JavaMisconfigured = TextResources.NoticeModelValidator_JavaMisconfigured;
		public static readonly string BadElasticsearchYamlFile = TextResources.NoticeModelValidator_BadElasticsearchYamlFile;
		public static readonly string NotAllModelsAreValid = TextResources.InstallationModelValidator_NotAllModelsAreValid;

		public InstallationModelValidator()
		{
			RuleFor(vm => vm.JavaInstalled).Must(b => b).WithMessage(JavaInstalled);

			RuleFor(vm => vm.JavaMisconfigured).Must(b => !b).WithMessage(JavaMisconfigured);

			RuleFor(vm => vm.BadElasticsearchYamlFile).Must(b => !b).WithMessage(BadElasticsearchYamlFile);

			RuleFor(vm => vm.SameVersionAlreadyInstalled).Must(b => !b).WithMessage(AlreadyInstalled);

			RuleFor(vm => vm.HigherVersionAlreadyInstalled).Must(b => !b).WithMessage(HigherVersionInstalled);

			RuleFor(vm => vm.Steps)
				.Must(steps => steps.All(s => s.IsValid))
				.When(vm => !vm.JavaInstalled
							&& !vm.JavaMisconfigured
							&& !vm.SameVersionAlreadyInstalled
							&& !vm.HigherVersionAlreadyInstalled
							&& !vm.BadElasticsearchYamlFile)
				.WithMessage(NotAllModelsAreValid);


		}
	}
}
