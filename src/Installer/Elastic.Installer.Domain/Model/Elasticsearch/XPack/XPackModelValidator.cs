using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.XPack
{
	public class XPackModelValidator : AbstractValidator<XPackModel>
	{
		public static readonly string NodeNameNotEmpty = TextResources.ConfigurationModelValidator_NodeName_NotEmpty;

		public XPackModelValidator()
		{
			RuleFor(c => c.XPackUsername)
				.NotEmpty().WithMessage("Username is required")
				.When(m => m.IsRelevant);

			RuleFor(c => c.XPackUserPassword)
				.NotEmpty().WithMessage("Password is required")
				.When(m => m.IsRelevant);
		}
	}
}