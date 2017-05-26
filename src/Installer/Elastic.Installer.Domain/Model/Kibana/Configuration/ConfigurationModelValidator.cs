using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Kibana.Configuration
{
	public class ConfigurationModelValidator : AbstractValidator<ConfigurationModel>
	{
		private static readonly string RouteFormatMustBeValid = TextResources.ConfigurationModelValidator_RouteFormatMustBeValid;
		private static readonly string DirectoryMustNotBeEmpty = TextResources.ConfigurationModelValidator_HostNameMustNotBeEmpty;
		private static readonly string BasePath = TextResources.ConfigurationModelValidator_BasePath;
		private static readonly string DefaultRoute = TextResources.ConfigurationModelValidator_DefaultRoute;

		public ConfigurationModelValidator()
		{
			RuleFor(vm => vm.HostName)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty()
				.WithMessage(DirectoryMustNotBeEmpty);

			RuleFor(vm => vm.BasePath)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.Must(BeAValidRoute)
				.WithMessage(RouteFormatMustBeValid, BasePath);

			RuleFor(vm => vm.DefaultRoute)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.Must(BeAValidRoute)
				.WithMessage(RouteFormatMustBeValid, DefaultRoute);
		}

		private static bool BeAValidRoute(string path) => string.IsNullOrEmpty(path) || path.StartsWith("/") && !path.EndsWith("/");
	}
}
