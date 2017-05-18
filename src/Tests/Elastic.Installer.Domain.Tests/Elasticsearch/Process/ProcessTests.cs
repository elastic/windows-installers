using System.IO;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
	public class ProcessTests
	{
		[Fact] public void StartsInDefaultJavaHomeByDefault() => AllDefaults().Start(p =>
		{
			var expectedJava = Path.Combine(DefaultJavaHome, @"bin\java.exe");
			p.ObservableProcess.BinaryCalled.Should().Be(expectedJava);
		});
	}
}