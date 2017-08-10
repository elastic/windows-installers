using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Elastic.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
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
			this.HomeDirectoryMachineVariable = esHome;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsHomeUserVariable(string esHome)
		{
			this.HomeDirectoryUserVariable = esHome;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsHomeProcessVariable(string esHome)
		{
			this.HomeDirectoryProcessVariable = esHome;
			return this;
		}
		
		public MockElasticsearchEnvironmentStateProvider ElasticsearchExecutable(string executable)
		{
			this.RunningExecutableLocation = executable;
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
		
		public MockElasticsearchEnvironmentStateProvider EsConfigMachineVariableOld(string esConfig)
		{
			this._esConfigMachineOld = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigUserVariableOld(string esConfig)
		{
			this._esConfigUserOld = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigProcessVariableOld(string esConfig)
		{
			this._esConfigProcessOld = esConfig;
			return this;
		}

		private string RunningExecutableLocation { get; set; }
		string IElasticsearchEnvironmentStateProvider.RunningExecutableLocation => this.RunningExecutableLocation;

		private string HomeDirectoryMachineVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryMachineVariable => this.HomeDirectoryMachineVariable;

		private string HomeDirectoryUserVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryUserVariable => this.HomeDirectoryUserVariable;

		private string HomeDirectoryProcessVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryProcessVariable => this.HomeDirectoryProcessVariable;

		private string _esConfigMachine;
		private string _esConfigMachineOld;
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryMachineVariable => this._esConfigMachine ?? this._esConfigMachineOld;
		private string _esConfigUser;
		private string _esConfigUserOld;
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryUserVariable => this._esConfigUser ?? this._esConfigUserOld;
		private string _esConfigProcess;
		private string _esConfigProcessOld;
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryProcessVariable => this._esConfigProcess ?? this._esConfigProcessOld;


		void IElasticsearchEnvironmentStateProvider.SetEsHomeEnvironmentVariable(string esHome)
		{
			this.LastSetEsHome = esHome;
		}

		void IElasticsearchEnvironmentStateProvider.SetEsConfigEnvironmentVariable(string esConfig)
		{
			this.LastSetEsConfig = esConfig;
		}

		public bool UnsetOldConfigVariableWasCalled { get; private set; }
		void IElasticsearchEnvironmentStateProvider.UnsetOldConfigVariable()
		{
			this.UnsetOldConfigVariableWasCalled = true;
		}
	}
}