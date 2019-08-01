using System;
using System.Collections.Generic;
using Elastic.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockElasticsearchEnvironmentStateProvider : IElasticsearchEnvironmentStateProvider
	{
		public MockElasticsearchEnvironmentStateProvider()
		{
			_systemVariables["TEMP"] = @"C:\Temp";
			_systemVariables["ES_TMPDIR"] = @"C:\Temp\elasticsearch";
		}
		
		private Dictionary<string, string> _systemVariables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, string> _userVariables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, string> _processVariables = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
		
		public string LastSetEsHome { get; set; }
		
		public string GetEnvironmentVariable(string variable)
		{
			if (_processVariables.TryGetValue(variable, out var v))
				return v;

			if (_userVariables.TryGetValue(variable, out v))
				return v;

			return _systemVariables.TryGetValue(variable, out v) ? v : null;
		}

		public bool TryGetEnv(string variable, out string value)
		{
			value = GetEnvironmentVariable(variable);
			return value != null;
		}
		
		public MockElasticsearchEnvironmentStateProvider EnvironmentVariables(Dictionary<string, string> variables)
		{
			foreach (var variable in variables)
				this._systemVariables[variable.Key] = variable.Value;

			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsHomeMachineVariable(string esHome)
		{
			this._systemVariables[ElasticsearchEnvironmentStateProvider.EsHome] = esHome;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsHomeUserVariable(string esHome)
		{
			this._userVariables[ElasticsearchEnvironmentStateProvider.EsHome] = esHome;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsHomeProcessVariable(string esHome)
		{
			this._processVariables[ElasticsearchEnvironmentStateProvider.EsHome] = esHome;
			return this;
		}
		
		public MockElasticsearchEnvironmentStateProvider ElasticsearchExecutable(string executable)
		{
			this.RunningExecutableLocation = executable;
			return this;
		}
		
		public MockElasticsearchEnvironmentStateProvider TempDirectory(string tempDirectory)
		{
			this._systemVariables["TEMP"] = tempDirectory;
			return this;
		}
		
		public MockElasticsearchEnvironmentStateProvider PrivateTempDirectory(string privateTempDirectory)
		{
			this._systemVariables["ES_TMPDIR"] = privateTempDirectory;
			return this;
		}

		public MockElasticsearchEnvironmentStateProvider EsConfigMachineVariable(string esConfig)
		{
			this.NewConfigDirectoryMachineVariable = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigUserVariable(string esConfig)
		{
			this._userVariables[ElasticsearchEnvironmentStateProvider.ConfDir] = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigProcessVariable(string esConfig)
		{
			this._processVariables[ElasticsearchEnvironmentStateProvider.ConfDir] = esConfig;
			return this;
		}
		
		public MockElasticsearchEnvironmentStateProvider EsConfigMachineVariableOld(string esConfig)
		{
			this.OldConfigDirectoryMachineVariable = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigUserVariableOld(string esConfig)
		{
			this._userVariables[ElasticsearchEnvironmentStateProvider.ConfDirOld] = esConfig;
			return this;
		}
		public MockElasticsearchEnvironmentStateProvider EsConfigProcessVariableOld(string esConfig)
		{
			this._processVariables[ElasticsearchEnvironmentStateProvider.ConfDirOld] = esConfig;
			return this;
		}

		private string RunningExecutableLocation { get; set; }
		string IElasticsearchEnvironmentStateProvider.RunningExecutableLocation => this.RunningExecutableLocation;

		string IElasticsearchEnvironmentStateProvider.TempDirectoryVariable => 
			this._systemVariables.TryGetValue("TEMP", out var v) ? v : null;

		string IElasticsearchEnvironmentStateProvider.PrivateTempDirectoryVariable => 
			this._systemVariables.TryGetValue("ES_TMPDIR", out var v) ? v : null;

		string IElasticsearchEnvironmentStateProvider.HomeDirectoryMachineVariable => 
			this._systemVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.EsHome, out var v) ? v : null;

		string IElasticsearchEnvironmentStateProvider.HomeDirectoryUserVariable => 
			this._userVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.EsHome, out var v) ? v : null;

		string IElasticsearchEnvironmentStateProvider.HomeDirectoryProcessVariable => 
			this._processVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.EsHome, out var v) ? v : null;

		public string OldConfigDirectoryMachineVariable
		{
			get => _systemVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.ConfDirOld, out var v) ? v : null;
			set => _systemVariables[ElasticsearchEnvironmentStateProvider.ConfDirOld] = value;
		}
		
		public string NewConfigDirectoryMachineVariable
		{
			get => _systemVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.ConfDir, out var v) ? v : null;
			set => _systemVariables[ElasticsearchEnvironmentStateProvider.ConfDir] = value;
		}
		
		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryMachineVariable => this.NewConfigDirectoryMachineVariable ?? this.OldConfigDirectoryMachineVariable;

		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryUserVariable => 
			(this._userVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.ConfDir, out var v) ? v : null) ??
			(this._userVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.ConfDirOld, out var v1) ? v1 : null);

		string IElasticsearchEnvironmentStateProvider.ConfigDirectoryProcessVariable => 
			(this._processVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.ConfDir, out var v) ? v : null) ?? 
			(this._processVariables.TryGetValue(ElasticsearchEnvironmentStateProvider.ConfDirOld, out var v1) ? v1 : null);
	}
}
