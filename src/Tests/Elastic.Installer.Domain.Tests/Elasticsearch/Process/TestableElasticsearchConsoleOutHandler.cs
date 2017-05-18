using System.Collections.Generic;
using Elastic.Installer.Domain.Process;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
	public class TestableElasticsearchConsoleOutHandler : IConsoleOutHandler
	{
		public List<ConsoleOut> Handled { get; } = new List<ConsoleOut>();
		public List<ConsoleOut> Written { get; } = new List<ConsoleOut>();

		public void Handle(ConsoleOut consoleOut) => this.Handled.Add(consoleOut);
		public void Write(ConsoleOut consoleOut) => this.Written.Add(consoleOut);
	}
}