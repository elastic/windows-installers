using System;
using System.Collections.Generic;
using System.IO;
using Elastic.Configuration.EnvironmentBased;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Elasticsearch.Process
{
	public class ElasticsearchObservableProcess : ObservableProcess
	{
		private readonly ElasticsearchEnvironmentConfiguration _env;

		public ElasticsearchObservableProcess(ElasticsearchEnvironmentConfiguration environmentConfiguration) => 
			this._env = environmentConfiguration;

		protected override Dictionary<string, string> ProcessVariables(bool warn)
		{
			var dict = base.ProcessVariables(warn);
			AlterProcessVariables(this._env, dict, warn);
			return dict;
		}

		protected override string GetCurrentWorkingDirectory() => _env.HomeDirectory;

		public static void AlterProcessVariables(ElasticsearchEnvironmentConfiguration env, Dictionary<string, string> dict, bool warn)
		{
			var esTemp = env.GetEnvironmentVariable("ES_TMPDIR") ?? Path.Combine(env.StateProvider.TempDirectoryVariable, "elasticsearch");
			esTemp = new DirectoryInfo(Path.GetFullPath(esTemp)).FullName; //convert short to long directory name
			dict["ES_TMPDIR"] = esTemp;
			dict["HOSTNAME"] = Environment.MachineName;

			var esPathLogs = env.GetEnvironmentVariable("ES_PATH_LOGS");
			if (string.IsNullOrEmpty(esPathLogs))
			{
				Console.WriteLine("ES_PATH_LOGS environment variable is null or empty, reading yaml file and setting value");
				var yaml = Path.Combine(env.ConfigDirectory, "elasticsearch.yml");
				if (File.Exists(yaml))
				{ 
					foreach (var line in File.ReadAllLines(yaml))
					{
						var logsPrefix = "path.logs:";
						if (line.Contains(logsPrefix))
						{
							esPathLogs = line.Replace(logsPrefix, string.Empty).Trim();
							break;
						}
					}
				}
			}
			dict["ES_PATH_LOGS"] = esPathLogs;
			Console.WriteLine($"Setting environment variable ES_PATH_LOGS to {esPathLogs}");

			const string javaToolOptionsKey = "JAVA_TOOL_OPTIONS";
			if (env.TryGetEnv(javaToolOptionsKey, out var toolOptions))
			{
				if (warn) Console.WriteLine($"warning: ignoring {javaToolOptionsKey}={toolOptions}");
				dict[javaToolOptionsKey] = null;
			}

			const string javaOptsKey = "JAVA_OPTS";
			if (env.TryGetEnv(javaOptsKey, out var javaOpts))
			{
				if (warn)
				{
					Console.WriteLine($"warning: ignoring {javaOptsKey}={javaOpts}");
					Console.WriteLine($"pass JVM parameters via ES_JAVA_OPTS");
				}
				dict[javaOptsKey] = null;
			}
		}
	}
}