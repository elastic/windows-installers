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
		
		public MockElasticsearchEnvironmentStateProvider TempDirectory(string tempDirectory)
		{
			this.TempDirectoryVariable = tempDirectory;
			return this;
		}

		public MockElasticsearchEnvironmentStateProvider EsConfigMachineVariable(string esConfig)
		{
			this.ConfigDirectoryMachineVariable = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigUserVariable(string esConfig)
		{
			this.ConfigDirectoryUserVariable = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigProcessVariable(string esConfig)
		{
			this.ConfigDirectoryProcessVariable = esConfig;
			return this;
		}
		
		private string RunningExecutableLocation { get; set; }
		string IElasticsearchEnvironmentStateProvider.RunningExecutableLocation => this.RunningExecutableLocation;

		private string TempDirectoryVariable { get; set; } = @"C:\Temp";
		string IElasticsearchEnvironmentStateProvider.TempDirectoryVariable => this.TempDirectoryVariable;

		private string HomeDirectoryMachineVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryMachineVariable => this.HomeDirectoryMachineVariable;

		private string HomeDirectoryUserVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryUserVariable => this.HomeDirectoryUserVariable;

		private string HomeDirectoryProcessVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.HomeDirectoryProcessVariable => this.HomeDirectoryProcessVariable;

		public string ConfigDirectoryMachineVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryMachineVariable => this.ConfigDirectoryMachineVariable;
		public string ConfigDirectoryUserVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryUserVariable => this.ConfigDirectoryUserVariable;
		public string ConfigDirectoryProcessVariable { get; set; }
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryProcessVariable => this.ConfigDirectoryProcessVariable;

		void IElasticsearchEnvironmentStateProvider.SetEsHomeEnvironmentVariable(string esHome)
		{
			this.LastSetEsHome = esHome;
			this.HomeDirectoryMachineVariable = esHome;
		}

		void IElasticsearchEnvironmentStateProvider.SetEsConfigEnvironmentVariable(string esConfig)
		{
			this.LastSetEsConfig = esConfig;
			this.ConfigDirectoryMachineVariable = esConfig;
		}
	}
}
