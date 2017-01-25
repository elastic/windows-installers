using System;
using System.ServiceProcess;
using System.Text;

namespace Elastic.Installer.Domain.Service.Elasticsearch
{
	public class ElasticsearchServiceConfiguration : ServiceConfiguration
	{
		public string ElasticsearchHomeDirectory { get; set; }
		public string ElasticsearchConfigDirectory { get; set; }

		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"- {nameof(Name)} = {Name}")
				.AppendLine($"- {nameof(DisplayName)} = {DisplayName}")
				.AppendLine($"- {nameof(StartMode)} = {Enum.GetName(typeof(ServiceStartMode), StartMode)}")
				.AppendLine($"- {nameof(ServiceAccount)} = {Enum.GetName(typeof(ServiceAccount), ServiceAccount)}")
				.AppendLine($"- {nameof(UserName)} = {UserName}")
				.AppendLine($"- {nameof(Password)} = {Password}")
				.AppendLine($"- {nameof(EventLogSource)} = {EventLogSource}")
				.AppendLine($"- {nameof(ElasticsearchHomeDirectory)} = {ElasticsearchHomeDirectory}")
				.AppendLine($"- {nameof(ElasticsearchConfigDirectory)} = {ElasticsearchConfigDirectory}")
				.AppendLine($"- {nameof(ExeLocation)} = {ExeLocation}")
				.ToString();
	}
}
