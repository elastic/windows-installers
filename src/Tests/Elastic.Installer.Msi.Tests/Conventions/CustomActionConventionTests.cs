using Elastic.Installer.Msi.CustomActions;
using FluentAssertions;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Elastic.Installer.Msi.Tests.Conventions
{
	public class CustomActionConventionTests
	{
		[Fact]
		public void ClassNameAndCustomActionStaticMethodNameShouldBeEqual()
		{
			var customActions = Assembly.GetAssembly(typeof(CustomAction)).GetTypes()
				.Where(t => t != typeof(CustomAction) && typeof(CustomAction).IsAssignableFrom(t) && !t.IsAbstract)
				.ToList();

			foreach(var customAction in customActions)
			{
				var staticMethods = customAction.GetMethods(BindingFlags.Public | BindingFlags.Static).ToList();
				var customActionMethod = staticMethods
					.SingleOrDefault(m => m
						.GetCustomAttributes()
						.Any(a => a.GetType().Name == "CustomActionAttribute"));

				customAction.Name.Replace("Action", "").Should().Be(customActionMethod.Name);
			}
		}
	}
}
