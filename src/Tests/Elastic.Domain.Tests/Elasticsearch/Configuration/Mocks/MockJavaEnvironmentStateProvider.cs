using Elastic.Configuration.EnvironmentBased.Java;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockJavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private string _esJavaHomeUserVariable;
		string IJavaEnvironmentStateProvider.EsJavaHomeUserVariable => _esJavaHomeUserVariable;

		private string _esJavaHomeMachineVariable;
		string IJavaEnvironmentStateProvider.EsJavaHomeMachineVariable => _esJavaHomeMachineVariable;

		private string _esJavaHomeProcessVariable;
		string IJavaEnvironmentStateProvider.EsJavaHomeProcessVariable => _esJavaHomeProcessVariable;

		public MockJavaEnvironmentStateProvider EsJavaHomeUserVariable(string path)
		{
			this._esJavaHomeUserVariable = path;
			return this;
		}

		public MockJavaEnvironmentStateProvider EsJavaHomeMachineVariable(string path)
		{
			this._esJavaHomeMachineVariable = path;
			return this;
		}

		public MockJavaEnvironmentStateProvider EsJavaHomeProcessVariable(string path)
		{
			this._esJavaHomeProcessVariable = path;
			return this;
		}

		private string _legacyJavaHomeUserVariable;
		string IJavaEnvironmentStateProvider.LegacyJavaHomeUserVariable => _legacyJavaHomeUserVariable;

		private string _legacyJavaHomeMachineVariable;
		string IJavaEnvironmentStateProvider.LegacyJavaHomeMachineVariable => _legacyJavaHomeMachineVariable;

		private string _legacyJavaHomeProcessVariable;
		string IJavaEnvironmentStateProvider.LegacyJavaHomeProcessVariable => _legacyJavaHomeProcessVariable;

		public MockJavaEnvironmentStateProvider LegacyJavaHomeUserVariable(string path)
		{
			this._legacyJavaHomeUserVariable = path;
			return this;
		}

		public MockJavaEnvironmentStateProvider LegacyJavaHomeMachineVariable(string path)
		{
			this._legacyJavaHomeMachineVariable = path;
			return this;
		}

		public MockJavaEnvironmentStateProvider LegacyJavaHomeProcessVariable(string path)
		{
			this._legacyJavaHomeProcessVariable = path;
			return this;
		}
	}
}
