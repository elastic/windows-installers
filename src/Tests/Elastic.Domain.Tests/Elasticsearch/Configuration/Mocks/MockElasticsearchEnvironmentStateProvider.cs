using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Elastic.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		private Dictionary<string, string> _mockVariables = new Dictionary<string, string>();

		public string LastSetEsHome { get; set; }
		public string LastSetEsConfig { get; set; }
		public string LastSetOldEsConfig { get; set; }

		public string GetEnvironmentVariable(string variable) => _mockVariables.TryGetValue(variable, out string v) ? v : null;
		public bool TryGetEnv(string variable, out string value)
		{
			value = GetEnvironmentVariable(variable);
			return value != null;
		}

		
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
		
		public MockElasticsearchEnvironmentStateProvider TempDirectory(string tempDirectory)
		{
			this.TempDirectoryVariable = tempDirectory;
			return this;
		}
		
		public MockElasticsearchEnvironmentStateProvider PrivateTempDirectory(string privateTempDirectory)
		{
			this.PrivateTempDirectoryVariable = privateTempDirectory;
			return this;
		}

		public MockElasticsearchEnvironmentStateProvider EsConfigMachineVariable(string esConfig)
		{
			this.NewConfigDirectoryMachineVariable = esConfig;
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
			this.OldConfigDirectoryMachineVariable = esConfig;
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

		private string TempDirectoryVariable { get; set; } = @"C:\Temp";
		string IElasticsearchEnvironmentStateProvider.TempDirectoryVariable => this.TempDirectoryVariable;
		
		private string PrivateTempDirectoryVariable { get; set; } = @"C:\Temp\elasticsearch";
		string IElasticsearchEnvironmentStateProvider.PrivateTempDirectoryVariable => this.PrivateTempDirectoryVariable;

		private string HomeDirectoryMachineVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryMachineVariable => this.HomeDirectoryMachineVariable;

		private string HomeDirectoryUserVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryUserVariable => this.HomeDirectoryUserVariable;

		private string HomeDirectoryProcessVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryProcessVariable => this.HomeDirectoryProcessVariable;

		public string OldConfigDirectoryMachineVariable { get; private set; }
		public string NewConfigDirectoryMachineVariable { get; private set; }
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryMachineVariable => this.NewConfigDirectoryMachineVariable ?? this.OldConfigDirectoryMachineVariable;
		private string _esConfigUser;
		private string _esConfigUserOld;
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryUserVariable => this._esConfigUser ?? this._esConfigUserOld;
		private string _esConfigProcess;
		private string _esConfigProcessOld;
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryProcessVariable => this._esConfigProcess ?? this._esConfigProcessOld;
	}
}
