using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Elastic.Configuration.EnvironmentBased.Java
{
	public class JavaConfiguration 
	{
		public static JavaConfiguration Default { get; } = 
			new JavaConfiguration(new JavaEnvironmentStateProvider(), new ElasticsearchEnvironmentStateProvider());

		private readonly IJavaEnvironmentStateProvider _javaStateProvider;
		private readonly IElasticsearchEnvironmentStateProvider _elasticsearchStateProvider;

		public JavaConfiguration(IJavaEnvironmentStateProvider javaStateProvider, IElasticsearchEnvironmentStateProvider elasticsearchStateProvider)
		{
			_javaStateProvider = javaStateProvider ?? new JavaEnvironmentStateProvider();
			_elasticsearchStateProvider = elasticsearchStateProvider ?? new ElasticsearchEnvironmentStateProvider();
		}

		public string JavaExecutable
		{
			get
			{
				try
				{
					return Path.Combine(this.JavaHomeCanonical, @"bin", "java.exe");
				}
				catch (Exception e)
				{
					throw new Exception(
						$"There was a problem constructing a path from the detected java home directory '{this.JavaHomeCanonical}'", e);
				}
			}
		}

		public string JavaHomeCanonical => JavaHomeCandidates.FirstOrDefault(j=>!string.IsNullOrWhiteSpace(j));

		private List<string> JavaHomeCandidates => new List<string> {
			_javaStateProvider.JavaHomeProcessVariable,
			_javaStateProvider.JavaHomeUserVariable,
			_javaStateProvider.JavaHomeMachineVariable,
			JavaFromEsHomeDirectory
		};

		private string JavaFromEsHomeDirectory
		{
			get
			{
				var esHome = _elasticsearchStateProvider.GetEnvironmentVariable(ElasticsearchEnvironmentStateProvider.EsHome)
					?? _elasticsearchStateProvider.RunningExecutableLocation;

				return esHome != null
					? Path.Combine(esHome, "jdk")
					: null;
			}
		}

		public bool JavaInstalled => !string.IsNullOrEmpty(this.JavaHomeCanonical);

		public bool JavaMisconfigured 
		{
			get
			{
				if (!JavaInstalled) return false;
				if (string.IsNullOrEmpty(_javaStateProvider.JavaHomeMachineVariable) && string.IsNullOrWhiteSpace(_javaStateProvider.JavaHomeUserVariable)) return false;
				if (string.IsNullOrWhiteSpace(_javaStateProvider.JavaHomeUserVariable)) return false;
				if (string.IsNullOrWhiteSpace(_javaStateProvider.JavaHomeMachineVariable)) return false;
				return _javaStateProvider.JavaHomeMachineVariable != _javaStateProvider.JavaHomeUserVariable;
			}
		}
			
		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"Java paths")
				.AppendLine($"- current = {JavaExecutable}")
				.AppendLine($"Java Candidates (in order of precedence)")
				.AppendLine($"- {nameof(_javaStateProvider.JavaHomeProcessVariable)} = {_javaStateProvider.JavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_javaStateProvider.JavaHomeUserVariable)} = {_javaStateProvider.JavaHomeUserVariable}")
				.AppendLine($"- {nameof(_javaStateProvider.JavaHomeMachineVariable)} = {_javaStateProvider.JavaHomeProcessVariable}")
				.AppendLine($"- {nameof(JavaFromEsHomeDirectory)} = {JavaFromEsHomeDirectory}")
				.AppendLine($"Java checks")
				.AppendLine($"- JAVA_HOME as machine and user variable = {JavaMisconfigured}")
				.ToString();

	}
}
