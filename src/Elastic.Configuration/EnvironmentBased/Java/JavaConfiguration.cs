using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Elastic.Configuration.EnvironmentBased.Java
{
	public class JavaConfiguration 
	{
		public static JavaConfiguration Default { get; } = new JavaConfiguration(new JavaEnvironmentStateProvider());

		private readonly IJavaEnvironmentStateProvider _stateProvider;

		public JavaConfiguration(IJavaEnvironmentStateProvider stateProvider)
		{
			_stateProvider = stateProvider ?? new JavaEnvironmentStateProvider();
		}

		public string JavaExecutable => Path.Combine(this.JavaHomeCanonical, @"bin", "java.exe");
		public string JavaHomeCanonical => JavaHomeCandidates.FirstOrDefault(j=>!string.IsNullOrWhiteSpace(j));

		private List<string> JavaHomeCandidates => new List<string> {
			_stateProvider.JavaHomeProcessVariable,
			_stateProvider.JavaHomeUserVariable,
			_stateProvider.JavaHomeMachineVariable,
			_stateProvider.JdkRegistry64,
			_stateProvider.JreRegistry64,
			_stateProvider.JdkRegistry32,
			_stateProvider.JreRegistry32
		};

		public bool JavaInstalled => !string.IsNullOrEmpty(this.JavaHomeCanonical);

		public bool Using32BitJava => JavaHomeCandidates.FindIndex(c => !string.IsNullOrWhiteSpace(c)) >= JavaHomeCandidates.Count - 2;

		public bool JavaMisconfigured 
		{
			get
			{
				if (!JavaInstalled) return false;
				if (string.IsNullOrEmpty(_stateProvider.JavaHomeMachineVariable) && string.IsNullOrWhiteSpace(_stateProvider.JavaHomeUserVariable)) return false;
				if (string.IsNullOrWhiteSpace(_stateProvider.JavaHomeUserVariable)) return false;
				if (string.IsNullOrWhiteSpace(_stateProvider.JavaHomeMachineVariable)) return false;
				return _stateProvider.JavaHomeMachineVariable != _stateProvider.JavaHomeUserVariable;
			}
		}
			
		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"Java paths")
				.AppendLine($"- current = {JavaExecutable}")
				.AppendLine($"Java Candidates (in order of precedence)")
				.AppendLine($"- {nameof(_stateProvider.JavaHomeProcessVariable)} = {_stateProvider.JavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_stateProvider.JavaHomeUserVariable)} = {_stateProvider.JavaHomeUserVariable}")
				.AppendLine($"- {nameof(_stateProvider.JavaHomeMachineVariable)} = {_stateProvider.JavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_stateProvider.JdkRegistry64)} = {_stateProvider.JdkRegistry64}")
				.AppendLine($"- {nameof(_stateProvider.JreRegistry64)} = {_stateProvider.JreRegistry64}")
				.AppendLine($"- {nameof(_stateProvider.JdkRegistry32)} = {_stateProvider.JdkRegistry32}")
				.AppendLine($"- {nameof(_stateProvider.JreRegistry32)} = {_stateProvider.JreRegistry32}")
				.AppendLine($"Java checks")
				.AppendLine($"- {nameof(Using32BitJava)} = {Using32BitJava}")
				.AppendLine($"- JAVA_HOME as machine and user variable = {JavaMisconfigured}")
				.ToString();

	}
}
