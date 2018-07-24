using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.XPack
{
	public class XPackModelValidator : AbstractValidator<XPackModel>
	{
		public static readonly string ElasticPasswordRequired = TextResources.XPackModelValidator_ElasticPasswordRequired;
		public static readonly string KibanaPasswordRequired = TextResources.XPackModelValidator_KibanaPasswordRequired;
		public static readonly string LogstashPasswordRequired = TextResources.XPackModelValidator_LogstashPasswordRequired;
		public static readonly string ElasticPasswordAtLeast6Characters = TextResources.XPackModelValidator_ElasticPasswordAtLeast6Characters;
		public static readonly string KibanaPasswordAtLeast6Characters = TextResources.XPackModelValidator_KibanaPasswordAtLeast6Characters;
		public static readonly string LogstashPasswordAtLeast6Characters = TextResources.XPackModelValidator_LogstashPasswordAtLeast6Characters;

		public XPackModelValidator()
		{
			RuleFor(c => c.ElasticUserPassword)
				.NotEmpty()
				.WithMessage(ElasticPasswordRequired)
				.When(NeedsPassword)
				.Length(6, int.MaxValue)
				.WithMessage(ElasticPasswordAtLeast6Characters)
				.When(NeedsPassword);
			
			RuleFor(c => c.KibanaUserPassword)
				.NotEmpty()
				.WithMessage(KibanaPasswordRequired)
				.When(NeedsPassword)
				.Length(6, int.MaxValue)
				.WithMessage(KibanaPasswordAtLeast6Characters)
				.When(NeedsPassword);
			
			RuleFor(c => c.LogstashSystemUserPassword)
				.NotEmpty()
				.WithMessage(LogstashPasswordRequired)
				.When(NeedsPassword)
				.Length(6, int.MaxValue)
				.WithMessage(LogstashPasswordAtLeast6Characters)
				.When(NeedsPassword);		
		}

		private static bool NeedsPassword(XPackModel m) => m.NeedsPasswords;
	}
}