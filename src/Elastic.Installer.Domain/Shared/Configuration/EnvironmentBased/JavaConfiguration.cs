using System.IO;
using System.Linq;

namespace Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased
{
	public class JavaConfiguration 
	{
		public static JavaConfiguration Default { get; } = new JavaConfiguration(new JavaEnvironmentStateProvider());

		private readonly IJavaEnvironmentStateProvider _stateProvider;

		public JavaConfiguration(IJavaEnvironmentStateProvider stateProvider)
		{
			_stateProvider = stateProvider ?? new JavaEnvironmentStateProvider();
		}

		public string JavaExecutable => Path.Combine(this.JavaHomeCanonical, @"bin\java.exe");
		public string JavaHomeCanonical => new [] {
				_stateProvider.JavaHomeMachine,
				_stateProvider.JavaHomeCurrentUser,
				_stateProvider.JavaHomeRegistry
			}
			.FirstOrDefault(j=>!string.IsNullOrWhiteSpace(j));

		public bool JavaInstalled => !string.IsNullOrEmpty(this.JavaHomeCanonical);

		public bool JavaMisconfigured 
		{
			get
			{
				if (!JavaInstalled) return false;
				if (string.IsNullOrEmpty(_stateProvider.JavaHomeMachine) && string.IsNullOrWhiteSpace(_stateProvider.JavaHomeCurrentUser)) return false;
				if (string.IsNullOrWhiteSpace(_stateProvider.JavaHomeCurrentUser)) return false;
				if (string.IsNullOrWhiteSpace(_stateProvider.JavaHomeMachine)) return false;
				return _stateProvider.JavaHomeMachine != _stateProvider.JavaHomeCurrentUser;
			}
		}

		public bool SetJavaHome(out string javaHome)
		{
			javaHome = _stateProvider.JavaHomeMachine;
			if (!string.IsNullOrEmpty(javaHome))
				return true; // already set at machine level, nothing to do

			var userValue = _stateProvider.JavaHomeCurrentUser;
			javaHome = !string.IsNullOrEmpty(userValue) ? userValue : _stateProvider.JavaHomeRegistry;
			if (string.IsNullOrEmpty(javaHome)) return false;
			_stateProvider.SetJavaHomeEnvironmentVariable(javaHome);
			return true;
		}



	}
}
