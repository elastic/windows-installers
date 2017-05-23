using System;
using Elastic.ProcessHosts.Process;
using FluentAssertions;
using Xunit;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ConsoleSession;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.ElasticsearchProcessTester;
using static Elastic.Installer.Domain.Tests.Elasticsearch.Process.TestableElasticsearchObservableProcess;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process.Observing
{
	public class HandlerTests
	{
		[Fact] public void HandlerSeesAllMessagesPriorToStarted() => AllDefaults(StartedSession)
			.Start(s =>
			{
				s.OutHandler.Handled.Count.Should().Be(StartedSession.Count);
			});

		[Fact] public void HandlerDoesNotSeeMessagesAfterStarted() => AllDefaults(new ConsoleSession(StartedSession)
			{"[x][INFO ][o.e.n.Node] [N] Additional message "}
			)
			.Start(s =>
			{
				s.OutHandler.Handled.Count.Should().Be(StartedSession.Count);
			});

		[Fact] public void StartedMessageFromWrongSectionIsIgnored() => AllDefaults(new ConsoleSession(BeforeStartedSession)
			{
				{"[x][INFO ][o.e.n.NodeXX] [N] started"}
			})
			.StartThrows((e, s) =>
			{
				s.OutHandler.Handled.Count.Should().Be(BeforeStartedSession.Count + 1);
				e.Should().BeOfType<StartupException>();
				e.Message.Should().Contain("Could not start process within");
			});

		[Fact] public void UncaughtExceptionThrowBeforeStartedThrows() => AllDefaults(new ConsoleSession(BeforeStartedSession)
			{
				{ThrowException}
			})
			.StartThrows((e, s) =>
			{
				s.OutHandler.Handled.Count.Should().Be(BeforeStartedSession.Count);
				e.Should().BeOfType<Exception>();
				e.Message.Should().Contain("funky");
			});

		[Fact] public void UncaughtExceptionThrowAfterStartedThrows() => AllDefaults(new ConsoleSession(StartedSession)
			{
				{ThrowException}
			})
			.StartThrows((e, s) =>
			{
				s.OutHandler.Handled.Count.Should().Be(StartedSession.Count);
				e.Should().BeOfType<Exception>();
				e.Message.Should().Contain("funky");
			});

		[Fact] public void StartupExceptionThrowBeforeStartedThrows() => AllDefaults(new ConsoleSession(BeforeStartedSession)
			{
				{ThrowStartupException}
			})
			.StartThrows((e, s) =>
			{
				s.OutHandler.Handled.Count.Should().Be(BeforeStartedSession.Count);
				e.Should().BeOfType<StartupException>();
			});

		[Fact] public void StartupExceptionThrowAfterStartedThrows() => AllDefaults(new ConsoleSession(StartedSession)
			{
				{ThrowStartupException}
			})
			.StartThrows((e, s) =>
			{
				s.OutHandler.Handled.Count.Should().Be(StartedSession.Count);
				e.Should().BeOfType<StartupException>();
			});
	}
}