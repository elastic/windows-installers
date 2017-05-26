using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ConsoleSession;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.TestableElasticsearchObservableProcess;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Observing
{
	public class WriterTests
	{
		[Fact]
		public void WriterWritesAllMessagesPriorToStarted() => AllDefaults(StartedSession)
			.Start(s =>
			{
				s.OutHandler.Written.Count.Should().Be(StartedSession.Count);
			});

		[Fact] public void WriterContinuousToWriteMessagesAfterStarted() => AllDefaults(new ConsoleSession(StartedSession)
			{
				{"[x][INFO ][o.e.n.Node] [N] Additional message "},
				{ EndProcess }
			})
			.RunToCompletion(s =>
			{
				s.OutHandler.Written.Count.Should().BeGreaterThan(StartedSession.Count);
			});


		[Fact] public void DoesNotWriteMessagesAfterOnCompleted() => AllDefaults(new ConsoleSession(StartedSession)
			{
				{"[x][INFO ][o.e.n.Node] [N] Additional message "},
				{ EndProcess },
				{"[x][INFO ][o.e.n.Node] [N] Additional message "},
			})
			.RunToCompletion(s =>
			{
				s.OutHandler.Written.Count.Should().Be(StartedSession.Count + 1);
			});

		[Fact] public void NoMessagesAreWrittenIfNotInteractive() => AllDefaults(new ConsoleSession(StartedSession)
			{"[x][INFO ][o.e.n.Node] [N] Additional message "}

			, interactive: false)
			.Start(s =>
			{
				s.OutHandler.Written.Count.Should().Be(0);
			});

		[Fact] public void RunsToCompletionEvenIfNotInteractive() => AllDefaults(new ConsoleSession(StartedSession)
			{
				{"[x][INFO ][o.e.n.Node] [N] Additional message "},
				{ EndProcess }
			}, interactive: false)
			.RunToCompletion(s =>
			{
				s.OutHandler.Written.Count.Should().Be(0);
			});
	}
}