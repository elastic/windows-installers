using System;

namespace Elastic.Installer.Domain
{
	public class StaticArgumentAttribute : ArgumentAttribute
	{
		public StaticArgumentAttribute(string name) : base(name)
		{
			IsStatic = true;
		}
	}

	public class SetPropertyActionArgumentAttribute : ArgumentAttribute
	{
		public SetPropertyActionArgumentAttribute(string name, string dynamicValue)
			: base(name)
		{
			IsDynamic = true;
			DynamicValue = dynamicValue;
		}
	}

	public class ArgumentAttribute : Attribute
	{
		/// <summary>
		/// The argument name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Whether a static value can be set against the property name in the generated MSI
		/// </summary>
		public bool IsStatic { get; protected set; }

		/// <summary>
		/// Whether a dynamic value can be calculated as part of the installation
		/// </summary>
		public bool IsDynamic { get; protected set; }

		/// <summary>
		/// Used to set the value at installation time
		/// </summary>
		public string DynamicValue { get; protected set; }

		public ArgumentAttribute(string name)
		{
			this.Name = name.ToUpperInvariant();
		}
	}
}