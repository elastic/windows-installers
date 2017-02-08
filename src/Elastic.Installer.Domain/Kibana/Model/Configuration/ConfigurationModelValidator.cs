using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using WixSharp;
using Elastic.Installer.Domain.Properties;

namespace Elastic.Installer.Domain.Kibana.Model.Configuration
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
				.Must(this.BeAValidRoute)
				.WithMessage(RouteFormatMustBeValid, BasePath);

			RuleFor(vm => vm.DefaultRoute)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.Must(this.BeAValidRoute)
				.WithMessage(RouteFormatMustBeValid, DefaultRoute);
		}

		public bool BeAValidRoute(string path) => path.IsNullOrEmpty() || path.StartsWith("/") && !path.EndsWith("/");
	}
}
