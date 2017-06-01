using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Elastic.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		private string _esHomeMachine;
		private string _esHomeUser;
		private string _esHomeProcess;
		private string _esExecutable;
		private string _esConfigMachine;
		private string _esConfigUser;
		private string _esConfigProcess;
		private Dictionary<string, string> _mockVariables = new Dictionary<string, string>();

		public string LastSetEsHome { get; set; }
		public string LastSetEsConfig { get; set; }

		public string GetEnvironmentVariable(string variable) => _mockVariables.TryGetValue(variable, out string v) ? v : null;

		public MockElasticsearchEnvironmentStateProvider EnvironmentVariables(Dictionary<string, string> variables)
		{
			this._mockVariables = variables;
			return this;
		}
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
		public MockElasticsearchEnvironmentStateProvider EsHomeProcessVariable(string esHome)
		{
			this._esHomeProcess = esHome;
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
		public MockElasticsearchEnvironmentStateProvider EsConfigProcessVariable(string esConfig)
		{
			this._esConfigProcess = esConfig;
			return this;
		}
		
		public string RunningExecutableLocation => this._esExecutable;

		public string HomeDirectoryUserVariable => this._esHomeUser;
		public string HomeDirectoryMachineVariable => this._esHomeMachine;
		public string HomeDirectoryProcessVariable => this._esHomeProcess;
		public string ConfigDirectoryUserVariable => this._esConfigUser;
		public string ConfigDirectoryMachineVariable => this._esConfigMachine;
		public string ConfigDirectoryProcessVariable => this._esConfigProcess;

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