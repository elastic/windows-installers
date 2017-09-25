using System;
using System.Linq;
using System.Net;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Plugins
{
	public class PluginsModelValidator : AbstractValidator<PluginsModel>
	{
		public static string NoInternet = TextResources.PluginsModelValidator_NoInternet;
		public static string InvalidHttpProxyHost = TextResources.PluginsModelValidator_InvalidHttpProxyHost;
		public static string InvalidHttpsProxyHost = TextResources.PluginsModelValidator_InvalidHttpsProxyHost;

		public PluginsModelValidator()
		{
			RuleFor(vm => vm.HasInternetConnection)
				.Must(v => v)
				.When(vm => vm.Plugins.Any())
				.WithMessage(NoInternet);

			RuleFor(vm => vm.HttpProxyHost)
				.Must(v => Uri.CheckHostName(v) != UriHostNameType.Unknown)
				.When(vm => !string.IsNullOrWhiteSpace(vm.HttpProxyHost))
				.WithMessage(InvalidHttpProxyHost);

			RuleFor(vm => vm.HttpsProxyHost)
				.Must(v => Uri.CheckHostName(v) != UriHostNameType.Unknown)
				.When(vm => !string.IsNullOrWhiteSpace(vm.HttpsProxyHost))
				.WithMessage(InvalidHttpsProxyHost);
		}
	}
}