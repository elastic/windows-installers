using Elastic.Configuration.EnvironmentBased.Java;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockJavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private string _javaHomeUserVariable;
		string IJavaEnvironmentStateProvider.JavaHomeUserVariable => _javaHomeUserVariable;
		private string _javaHomeMachineVariable;
		string IJavaEnvironmentStateProvider.JavaHomeMachineVariable => _javaHomeMachineVariable;
		private string _javaHomeProcessVariable;
		string IJavaEnvironmentStateProvider.JavaHomeProcessVariable => _javaHomeProcessVariable;

		public MockJavaEnvironmentStateProvider JavaHomeUserVariable(string path)
		{
			this._javaHomeUserVariable = path;
			return this;
		}

		public MockJavaEnvironmentStateProvider JavaHomeMachineVariable(string path)
		{
			this._javaHomeMachineVariable = path;
			return this;
		}
		public MockJavaEnvironmentStateProvider JavaHomeProcessVariable(string path)
		{
			this._javaHomeProcessVariable = path;
			return this;
		}
	}
}