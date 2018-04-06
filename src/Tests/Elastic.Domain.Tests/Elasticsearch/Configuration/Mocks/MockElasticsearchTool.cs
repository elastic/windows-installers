using Elastic.ProcessHosts.Elasticsearch.Process;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks
{
	public delegate bool ElasticsearchToolStart(string[] args, out string stdOut);

	public class MockElasticsearchTool : IElasticsearchTool
	{
		private readonly ElasticsearchToolStart _start;
		private string[] _arguments;
		private string _stdOut;

		private static bool DefaultStart(string[] arguments, out string stdOut)
		{
			stdOut = string.Empty;
			return true;
		}

		public MockElasticsearchTool() : this(DefaultStart) {}

		public MockElasticsearchTool(ElasticsearchToolStart start) => _start = start;

		public bool Start(string[] arguments, out string output)
		{
			_arguments = arguments;
			var result = _start(arguments, out output);
			_stdOut = output;
			return result;
		}

		public string[] PassedArguments => _arguments;

		public string StandardOutput => _stdOut;
	}
}