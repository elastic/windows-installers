

using System.IO;
using System.Linq;
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
		private string _jdkRegistry64;
		string IJavaEnvironmentStateProvider.JdkRegistry64 => _jdkRegistry64;
		private string _jdkRegistry32;
		string IJavaEnvironmentStateProvider.JdkRegistry32 => _jdkRegistry32;
		private string _jreRegistry64;
		string IJavaEnvironmentStateProvider.JreRegistry64 => _jreRegistry64; 
		private string _jreRegistry32;
		string IJavaEnvironmentStateProvider.JreRegistry32 => _jreRegistry32;

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

		public MockJavaEnvironmentStateProvider JdkRegistry64(string path)
		{
			this._jdkRegistry64 = path;
			return this;
		}
		public MockJavaEnvironmentStateProvider JreRegistry64(string path)
		{
			this._jreRegistry64 = path;
			return this;
		}
		public MockJavaEnvironmentStateProvider JdkRegistry32(string path)
		{
			this._jdkRegistry32 = path;
			return this;
		}
		public MockJavaEnvironmentStateProvider JreRegistry32(string path)
		{
			this._jreRegistry32 = path;
			return this;
		}
	}
}