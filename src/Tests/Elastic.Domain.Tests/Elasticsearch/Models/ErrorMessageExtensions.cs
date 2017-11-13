using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using FluentValidation.Results;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Models
{
	public static class ErrorMessageExtensions
	{
		public static void ShouldHaveErrors(this IList<ValidationFailure> f, params string[] messages)
		{
			f.Should().HaveCount(messages.Length, "only has errors {0}", f.ToUnitTestMessage());
			foreach (var m in messages)
				f.Should().Contain(e => e.HasErrorMessage(m), "only has errors {0}", f.ToUnitTestMessage());
		}

		public static bool HasErrorMessage(this ValidationFailure f, string message) => 
			f.ErrorMessage == "• " + message || f.ErrorMessage == message;

		public static string ToUnitTestMessage(this IList<ValidationFailure> f) => 
			f.Aggregate(new StringBuilder(), (sb, v) => sb.AppendLine($"• '{v.PropertyName}': {v.ErrorMessage}"), (sb) => sb.ToString());
	}
}