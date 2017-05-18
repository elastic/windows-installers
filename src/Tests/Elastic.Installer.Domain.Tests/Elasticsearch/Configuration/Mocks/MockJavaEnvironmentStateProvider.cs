

using System.IO;
using System.Linq;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public class MockJavaEnvironmentStateProvider : IJavaEnvironmentStateProvider
	{
		private string _javaHomeCurrentUser;
		string IJavaEnvironmentStateProvider.JavaHomeCurrentUser => _javaHomeCurrentUser;
		private string _javaHomeMachine;
		string IJavaEnvironmentStateProvider.JavaHomeMachine => _javaHomeMachine;
		private string _javaHomeRegistry;
		string IJavaEnvironmentStateProvider.JavaHomeRegistry => _javaHomeRegistry;

		public string JavaExecutable => Path.Combine(this.JavaHomeCanonical, @"bin\java.exe");
		private IJavaEnvironmentStateProvider _t => this;
		public string JavaHomeCanonical => new [] {_t.JavaHomeMachine, _t.JavaHomeCurrentUser, _t.JavaHomeRegistry}
			.FirstOrDefault(j=>!string.IsNullOrWhiteSpace(j));

		public void SetJavaHomeEnvironmentVariable(string javaHome) { }


		public MockJavaEnvironmentStateProvider JavaHomeCurrentUser(string path)
		{
			this._javaHomeCurrentUser = path;
			return this;
		}

		public MockJavaEnvironmentStateProvider JavaHomeMachine(string path)
		{
			this._javaHomeMachine = path;
			return this;
		}

		public MockJavaEnvironmentStateProvider JavaHomeRegistry(string path)
		{
			this._javaHomeRegistry = path;
			return this;
		}
	}
}