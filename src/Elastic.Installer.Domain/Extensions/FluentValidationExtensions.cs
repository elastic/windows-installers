namespace Elastic.Installer.Domain.Extensions
{
	public static class FluentValidationExtensions
	{
		public static string ValidationMessage(this string message)
		{
			return "\u2022 " + message;
		}
	}
}