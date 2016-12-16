using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		private string _homeDirectory;
		public string HomeDirectory => _homeDirectory;
		private string _configDirectory;
		public string ConfigDirectory => _configDirectory;

		public string LastSetEsHome { get; set; }
		public string LastSetEsConfig { get; set; }

		public MockElasticsearchEnvironmentStateProvider HomeDirectoryEnvironmentVariable(string esHome)
		{
			this._homeDirectory = esHome;
			return this;
		}

		public MockElasticsearchEnvironmentStateProvider ConfigDirectoryEnvironmentVariable(string esConfig)
		{
			this._configDirectory = esConfig;
			return this;
		}

		public void SetEsHomeEnvironmentVariable(string esHome)
		{
			this.LastSetEsHome = esHome;
		}

		public void SetEsConfigEnvironmentVariable(string esConfig)
		{
			this.LastSetEsConfig = esConfig;
		}
	}
}