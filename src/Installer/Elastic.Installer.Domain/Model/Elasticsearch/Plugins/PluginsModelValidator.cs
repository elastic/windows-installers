using System.Linq;
using System.Net;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Plugins
{
	public class PluginsModelValidator : AbstractValidator<PluginsModel>
	{
		private static readonly string NoInternet = TextResources.PluginsModelValidator_NoInternet;

		public PluginsModelValidator()
		{
			RuleFor(vm => vm.HasInternetConnection)
				.Must(v => v)
				.When(vm => vm.Plugins.Any())
				.WithMessage(NoInternet);
		}
	}
}