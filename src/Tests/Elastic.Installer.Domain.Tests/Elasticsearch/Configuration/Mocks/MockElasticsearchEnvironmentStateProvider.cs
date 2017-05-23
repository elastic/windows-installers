using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		private string _esHomeMachine;
		private string _esHomeUser;
		private string _esExecutable;
		private string _esConfigMachine;
		private string _esConfigUser;

		public string LastSetEsHome { get; set; }
		public string LastSetEsConfig { get; set; }

		public MockElasticsearchEnvironmentStateProvider EsHomeMachineVariable(string esHome)
		{
			this._esHomeMachine = esHome;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsHomeUserVariable(string esHome)
		{
			this._esHomeUser = esHome;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider ElasticsearchExecutable(string executable)
		{
			this._esExecutable = executable;
			return this;
		}

		public MockElasticsearchEnvironmentStateProvider EsConfigMachineVariable(string esConfig)
		{
			this._esConfigMachine = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigUserVariable(string esConfig)
		{
			this._esConfigUser = esConfig;
			return this;
		}

		public string HomeDirectoryUserVariable => this._esHomeUser;
		public string HomeDirectoryMachineVariable => this._esHomeMachine;
		public string RunningExecutableLocation => this._esExecutable;
		public string ConfigDirectoryUserVariable => this._esConfigUser;
		public string ConfigDirectoryMachineVariable => this._esConfigMachine;

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