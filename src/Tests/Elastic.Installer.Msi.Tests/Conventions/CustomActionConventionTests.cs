using Elastic.Installer.Msi.CustomActions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
					.Where(m => m
						.GetCustomAttributes()
						.Any(a => a.GetType().Name == "CustomActionAttribute")
					)
					.SingleOrDefault();

				customAction.Name.Replace("Action", "").Should().Be(customActionMethod.Name);
			}
		}
	}
}
