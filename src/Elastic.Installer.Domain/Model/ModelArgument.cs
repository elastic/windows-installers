using System;
using System.Reflection;

namespace Elastic.Installer.Domain.Model
{
	public class ModelArgument
	{
		public ArgumentAttribute Attribute { get; internal set; }

		public PropertyInfo PropertyInfo { get; set; }

		public Type PropertyType { get; set; }

		public IValidatableReactiveObject Model { get; set; }

		public string Key { get; set; }

		public string Value { get; set; }
	}
}