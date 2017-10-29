using System;
using System.Linq;
using Elastic.Installer.Domain.Properties;
using FluentValidation;

namespace Elastic.Installer.Domain.Model.Elasticsearch.Plugins
{
	public class PluginsModelValidator : AbstractValidator<PluginsModel>
	{
		public static string NoInternet = TextResources.PluginsModelValidator_NoInternet;
		public static string InvalidHttpProxyHost = TextResources.PluginsModelValidator_InvalidHttpProxyHost;
		public static string HttpProxyHostRequiredWhenPortSpecified = TextResources.PluginsModelValidator_HttpProxyHostRequiredWhenPortSpecified;
		public static string InvalidHttpProxyPort = TextResources.PluginsModelValidator_InvalidHttpProxyPort;

		public static string InvalidHttpsProxyHost = TextResources.PluginsModelValidator_InvalidHttpsProxyHost;
		public static string HttpsProxyHostRequiredWhenPortSpecified = TextResources.PluginsModelValidator_HttpsProxyHostRequiredWhenPortSpecified;
		public static string InvalidHttpsProxyPort = TextResources.PluginsModelValidator_InvalidHttpsProxyPort;

		public PluginsModelValidator()
		{
			RuleFor(vm => vm.HasInternetConnection)
				.Must(v => !v.HasValue || v.Value)
				.When(vm => vm.Plugins.Any())
				.WithMessage(NoInternet);

			RuleFor(vm => vm.HttpProxyHost)
				.NotEmpty()
				.When(vm => vm.HttpProxyPort.HasValue)
				.WithMessage(HttpProxyHostRequiredWhenPortSpecified);

			RuleFor(vm => vm.HttpProxyHost)
				.Must(v => Uri.CheckHostName(v) != UriHostNameType.Unknown)
				.When(vm => !string.IsNullOrWhiteSpace(vm.HttpProxyHost))
				.WithMessage(InvalidHttpProxyHost);

			RuleFor(vm => vm.HttpProxyPort)
				.GreaterThanOrEqualTo(PluginsModel.HttpPortMinimum)
				.WithMessage(string.Format(InvalidHttpProxyPort, PluginsModel.HttpPortMinimum, PluginsModel.PortMaximum))
				.LessThanOrEqualTo(PluginsModel.PortMaximum)
				.WithMessage(string.Format(InvalidHttpProxyPort, PluginsModel.HttpPortMinimum, PluginsModel.PortMaximum));

			RuleFor(vm => vm.HttpsProxyHost)
				.NotEmpty()
				.When(vm => vm.HttpsProxyPort.HasValue)
				.WithMessage(HttpsProxyHostRequiredWhenPortSpecified);

			RuleFor(vm => vm.HttpsProxyHost)
				.Must(v => Uri.CheckHostName(v) != UriHostNameType.Unknown)
				.When(vm => !string.IsNullOrWhiteSpace(vm.HttpsProxyHost))
				.WithMessage(InvalidHttpsProxyHost);

			RuleFor(vm => vm.HttpsProxyPort)
				.GreaterThanOrEqualTo(PluginsModel.HttpsPortMinimum)
				.WithMessage(string.Format(InvalidHttpsProxyPort, PluginsModel.HttpsPortMinimum, PluginsModel.PortMaximum))
				.LessThanOrEqualTo(PluginsModel.PortMaximum)
				.WithMessage(string.Format(InvalidHttpsProxyPort, PluginsModel.HttpsPortMinimum, PluginsModel.PortMaximum));
		}
	}
}