using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Kibana.Connecting
{
	public class ConnectingModelValidator : AbstractValidator<ConnectingModel>
	{
		public ConnectingModelValidator()
		{
			RuleFor(vm => vm.Url)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty()
				.WithMessage(TextResources.ConnectingModelValidator_ElasticsearchUrlMustBeSpecified);

			RuleFor(vm => vm.IndexName)
				.Cascade(CascadeMode.StopOnFirstFailure)
				.NotEmpty()
				.WithMessage(TextResources.ConnectingModelValidator_IndexMustBeSpecified);
		}
	}
}
