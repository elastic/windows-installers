using Elastic.Installer.Domain.Properties;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain.Kibana.Model.Connecting
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
