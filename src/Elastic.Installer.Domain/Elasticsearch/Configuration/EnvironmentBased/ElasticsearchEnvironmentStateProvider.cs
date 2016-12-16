using System;

namespace Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased
{

	public interface IElasticsearchEnvironmentStateProvider
	{
		string HomeDirectory { get; }
		string ConfigDirectory { get; }

		void SetEsHomeEnvironmentVariable(string esHome);
		void SetEsConfigEnvironmentVariable(string esConfig);
	}

	public class ElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		public static ElasticsearchEnvironmentStateProvider Default { get; } = new ElasticsearchEnvironmentStateProvider();

		public string HomeDirectory =>
			Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.Machine)
			?? Environment.GetEnvironmentVariable("ES_HOME", EnvironmentVariableTarget.User);

		public string ConfigDirectory =>
			Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.Machine)
			?? Environment.GetEnvironmentVariable("ES_CONFIG", EnvironmentVariableTarget.User);

		public void SetEsHomeEnvironmentVariable(string esHome) =>
			Environment.SetEnvironmentVariable("ES_HOME", esHome, EnvironmentVariableTarget.Machine);

		public void SetEsConfigEnvironmentVariable(string esConfig) =>
			Environment.SetEnvironmentVariable("ES_CONFIG", esConfig, EnvironmentVariableTarget.Machine);

	}
}