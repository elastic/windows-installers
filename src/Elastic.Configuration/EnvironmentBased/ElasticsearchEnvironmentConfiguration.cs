using System.IO;
using System.Linq;
using System.Text;

namespace Elastic.Configuration.EnvironmentBased
{
	public class ElasticsearchEnvironmentConfiguration
	{
		public static ElasticsearchEnvironmentConfiguration Default { get; } = 
			new ElasticsearchEnvironmentConfiguration(new ElasticsearchEnvironmentStateProvider());

		public IElasticsearchEnvironmentStateProvider StateProvider { get; }

		public ElasticsearchEnvironmentConfiguration(IElasticsearchEnvironmentStateProvider stateProvider) => 
			StateProvider = stateProvider ?? new ElasticsearchEnvironmentStateProvider();

		private string HomeDirectoryInferred
		{
			get
			{
				var codeBase = StateProvider.RunningExecutableLocation;
				if (string.IsNullOrWhiteSpace(codeBase)) return null;
				var codeBaseFolder = new DirectoryInfo(Path.GetDirectoryName(codeBase));
				var directoryInfo = codeBaseFolder.Parent;
				return directoryInfo?.FullName;
			}
		}

		public string TargetInstallationDirectory => new []
			{
				StateProvider.HomeDirectoryProcessVariable,
				StateProvider.HomeDirectoryUserVariable,
				StateProvider.HomeDirectoryMachineVariable,
			}
			.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));

		public string TargetInstallationConfigDirectory => new []
			{
				StateProvider.ConfigDirectoryProcessVariable,
				StateProvider.ConfigDirectoryUserVariable,
				StateProvider.ConfigDirectoryMachineVariable,
			}
			.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));

		public string HomeDirectory => new []
			{
				StateProvider.HomeDirectoryProcessVariable,
				StateProvider.HomeDirectoryUserVariable,
				StateProvider.HomeDirectoryMachineVariable,
				this.HomeDirectoryInferred
			}
			.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));

		public string ConfigDirectory
		{
			get
			{
				var variableOption = new []
				{
					StateProvider.ConfigDirectoryProcessVariable,
					StateProvider.ConfigDirectoryUserVariable,
					StateProvider.ConfigDirectoryMachineVariable,
				}.FirstOrDefault(v=>!string.IsNullOrWhiteSpace(v));
				if (!string.IsNullOrWhiteSpace(variableOption)) return variableOption;

				var homeDir = this.HomeDirectory;
				return string.IsNullOrEmpty(homeDir) ? null : Path.Combine(homeDir, "config");
			}
		}

		public string PrivateTempDirectory => this.StateProvider.PrivateTempDirectoryVariable ?? Path.Combine(this.StateProvider.TempDirectoryVariable, "elasticsearch");

		public string PreviousInstallationDirectory => new[]
			{
				// TODO: Get the value of ES_HOME env var for the elasticsearch.exe process, if running
				StateProvider.HomeDirectoryUserVariable,
				StateProvider.HomeDirectoryMachineVariable,
			}
			.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

		public string GetEnvironmentVariable(string variable) => this.StateProvider.GetEnvironmentVariable(variable);

		public override string ToString() =>
			new StringBuilder()
				.AppendLine($"{ElasticsearchEnvironmentStateProvider.EsHome} (in order of precedence)")
				.AppendLine($"- {nameof(StateProvider.HomeDirectoryProcessVariable)} = {StateProvider.HomeDirectoryProcessVariable}")
				.AppendLine($"- {nameof(StateProvider.HomeDirectoryUserVariable)} = {StateProvider.HomeDirectoryUserVariable}")
				.AppendLine($"- {nameof(StateProvider.HomeDirectoryMachineVariable)} = {StateProvider.HomeDirectoryMachineVariable}")
				.AppendLine($"- From executable location = {HomeDirectoryInferred}")
		
				.AppendLine($"{ElasticsearchEnvironmentStateProvider.ConfDir} (in order of precedence)")
				.AppendLine($"- {nameof(StateProvider.ConfigDirectoryProcessVariable)} = {StateProvider.ConfigDirectoryProcessVariable}")
				.AppendLine($"- {nameof(StateProvider.ConfigDirectoryUserVariable)} = {StateProvider.ConfigDirectoryUserVariable}")
				.AppendLine($"- {nameof(StateProvider.ConfigDirectoryMachineVariable)} = {StateProvider.ConfigDirectoryMachineVariable}")
				.AppendLine($"- Fallback to ES_HOME = {Path.Combine(HomeDirectory, "config")}")
				.ToString();

		public bool TryGetEnv(string variable, out string value) => StateProvider.TryGetEnv(variable, out value);
	}
}