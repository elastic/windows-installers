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

		public JavaConfiguration(
			IJavaEnvironmentStateProvider javaStateProvider,
			IElasticsearchEnvironmentStateProvider elasticsearchStateProvider)
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

		public string JavaHomeCanonical => JavaHomeCandidates.FirstOrDefault(j => !string.IsNullOrWhiteSpace(j));

		private List<string> JavaHomeCandidates => new List<string> {
			_javaStateProvider.EsJavaHomeProcessVariable,
			_javaStateProvider.EsJavaHomeUserVariable,
			_javaStateProvider.EsJavaHomeMachineVariable,
			_javaStateProvider.LegacyJavaHomeProcessVariable,
			_javaStateProvider.LegacyJavaHomeUserVariable,
			_javaStateProvider.LegacyJavaHomeMachineVariable,
			JavaFromEsHomeDirectory
		};

		private string JavaFromEsHomeDirectory
		{
			get
			{
				var esHome = _elasticsearchStateProvider.GetEnvironmentVariable(ElasticsearchEnvironmentStateProvider.EsHome)
					?? _elasticsearchStateProvider.RunningExecutableLocation;

				return esHome != null
					? Path.Combine(esHome, "jdk").Trim()
					: null;
			}
		}

		public bool JavaInstalled => !string.IsNullOrEmpty(this.JavaHomeCanonical);

		public bool JavaMisconfigured
		{
			get
			{
				if (!JavaInstalled)
					return false;

				// All empty
				if (string.IsNullOrEmpty(_javaStateProvider.EsJavaHomeMachineVariable)
					&& string.IsNullOrEmpty(_javaStateProvider.EsJavaHomeUserVariable)
					&& string.IsNullOrEmpty(_javaStateProvider.LegacyJavaHomeMachineVariable)
					&& string.IsNullOrEmpty(_javaStateProvider.LegacyJavaHomeUserVariable))
				{
					return false;
				}

				// ES_JAVA_HOME
				if (string.IsNullOrEmpty(_javaStateProvider.EsJavaHomeUserVariable)
					&& string.IsNullOrEmpty(_javaStateProvider.LegacyJavaHomeUserVariable))
				{
					return false;
				}

				if (string.IsNullOrEmpty(_javaStateProvider.EsJavaHomeMachineVariable)
					&& string.IsNullOrEmpty(_javaStateProvider.LegacyJavaHomeMachineVariable))
				{
					return false;
				}

				if (string.Compare(
					_javaStateProvider.EsJavaHomeMachineVariable,
					_javaStateProvider.EsJavaHomeUserVariable,
					StringComparison.OrdinalIgnoreCase) == 0
					&& !string.IsNullOrEmpty(_javaStateProvider.EsJavaHomeMachineVariable))
				{ 
					return false; 
				}

				if (string.Compare(
						_javaStateProvider.LegacyJavaHomeUserVariable,
						_javaStateProvider.LegacyJavaHomeMachineVariable,
						StringComparison.OrdinalIgnoreCase) == 0
					&& !string.IsNullOrEmpty(_javaStateProvider.LegacyJavaHomeUserVariable))
				{
					return false;
				}

				// Misconfiguration
				return true;
			}
		}

		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"Java paths")
				.AppendLine($"- current = {JavaExecutable}")
				.AppendLine($"Java Candidates (in order of precedence)")
				.AppendLine($"- {nameof(_javaStateProvider.EsJavaHomeProcessVariable)} = {_javaStateProvider.EsJavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_javaStateProvider.EsJavaHomeUserVariable)} = {_javaStateProvider.EsJavaHomeUserVariable}")
				.AppendLine($"- {nameof(_javaStateProvider.EsJavaHomeMachineVariable)} = {_javaStateProvider.EsJavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_javaStateProvider.LegacyJavaHomeProcessVariable)} = {_javaStateProvider.LegacyJavaHomeProcessVariable}")
				.AppendLine($"- {nameof(_javaStateProvider.LegacyJavaHomeUserVariable)} = {_javaStateProvider.LegacyJavaHomeUserVariable}")
				.AppendLine($"- {nameof(_javaStateProvider.LegacyJavaHomeMachineVariable)} = {_javaStateProvider.LegacyJavaHomeProcessVariable}")
				.AppendLine($"- {nameof(JavaFromEsHomeDirectory)} = {JavaFromEsHomeDirectory}")
				.AppendLine($"Java checks")
				.AppendLine($"- ES_JAVA_HOME/JAVA_HOME as machine and user variable = {JavaMisconfigured}")
				.ToString();

	}
}
