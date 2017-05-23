using System;
using System.ServiceProcess;
using System.Text;

namespace Elastic.Installer.Domain.Service
{
	public abstract class ServiceConfiguration
	{
		public string HomeDirectory { get; set; }
		public string ConfigDirectory { get; set; }
		public string ExeLocation { get; set; }
		public string EventLogSource { get; set; }
		public ServiceAccount ServiceAccount { get; set; }
		public string Name { get; set; }
		public string DisplayName { get; set; }
		public string Description { get; set; }
		public ServiceStartMode StartMode { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }

		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"- {nameof(Name)} = {Name}")
				.AppendLine($"- {nameof(DisplayName)} = {DisplayName}")
				.AppendLine($"- {nameof(StartMode)} = {Enum.GetName(typeof(ServiceStartMode), StartMode)}")
				.AppendLine($"- {nameof(ServiceAccount)} = {Enum.GetName(typeof(ServiceAccount), ServiceAccount)}")
				.AppendLine($"- {nameof(UserName)} = {UserName}")
				.AppendLine($"- {nameof(Password)} = {!string.IsNullOrWhiteSpace(Password)}")
				.AppendLine($"- {nameof(EventLogSource)} = {EventLogSource}")
				.AppendLine($"- {nameof(HomeDirectory)} = {HomeDirectory}")
				.AppendLine($"- {nameof(ConfigDirectory)} = {ConfigDirectory}")
				.AppendLine($"- {nameof(ExeLocation)} = {ExeLocation}")
				.ToString();
	}
}
