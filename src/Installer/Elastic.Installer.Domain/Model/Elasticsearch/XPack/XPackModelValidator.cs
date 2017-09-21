using System;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.XPack
{
	public class XPackModelValidator : AbstractValidator<XPackModel>
	{
		public static readonly string NodeNameNotEmpty = TextResources.ConfigurationModelValidator_NodeName_NotEmpty;

		public XPackModelValidator()
		{
			RuleFor(c => c.ElasticUserPassword)
				.NotEmpty().WithMessage("`elastic` user's password is required")
				.When(NeedsPassword);
			
			RuleFor(c => c.KibanaUserPassword)
				.NotEmpty().WithMessage("`kibana` user's password is required")
				.When(NeedsPassword);
			
			RuleFor(c => c.LogstashSystemUserPassword)
				.NotEmpty().WithMessage("`logstash_system` user's password is required")
				.When(NeedsPassword);
			
		}

		private static bool NeedsPassword(XPackModel m) => m.NeedsPassword;
	}
}