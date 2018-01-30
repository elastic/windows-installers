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
		public SetPropertyActionArgumentAttribute(string name, string dynamicValue, bool uppercase = true)
			: base(name, uppercase)
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

		/// <summary>
		/// Denotes that the Property is not logged during installation
		/// </summary>
		public bool IsHidden { get; set; }

		/// <summary>
		/// Denotes that the Property can be passed to the server side when 
		/// doing a managed installation with elevated privileges.
		/// </summary>
		public bool IsSecure { get; set; }

		/// <summary>
		/// Whether the value should be persisted in the registry to use when
		/// performing upgrades or uninstalls
		/// </summary>
		public bool PersistInRegistry { get; set; }

		public ArgumentAttribute(string name, bool uppercase = true) => 
			this.Name = uppercase ? name.ToUpperInvariant() : name;
	}
}