﻿using System;
using System.Linq;
using System.Threading;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Tests.Elasticsearch.Configuration.Mocks;
using Elastic.ProcessHosts.Elasticsearch.Process;
using Elastic.ProcessHosts.Process;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
	public class ElasticsearchProcessTester
	{
		public ElasticsearchProcess Process { get; set; }
		public TestableElasticsearchConsoleOutHandler OutHandler { get; set; }
		public TestableElasticsearchObservableProcess ObservableProcess { get; set; }

		public static string DefaultJavaHome { get; } = @"C:\Java";
		public static string DefaultEsHome { get; } = LocationsModel.DefaultInstallationDirectory;

		public ElasticsearchProcessTester(Func<ElasticsearchProcessTesterStateProvider, ElasticsearchProcessTesterStateProvider> setup)
		{
			var state = setup(new ElasticsearchProcessTesterStateProvider());

			this.ObservableProcess = new TestableElasticsearchObservableProcess(state.ConsoleSessionState, state.InteractiveEnvironment);
			this.OutHandler = state.OutHandler;

			this.Process = new ElasticsearchProcess(
				this.ObservableProcess,
				this.OutHandler,
				state.FileSystemState,
				state.ElasticsearchConfigState,
				state.JavaConfigState,
				state.CompletionHandle,
				state.ProcessArgs);
		}

		public static ElasticsearchProcessTester AllDefaults(params string[] args) => new ElasticsearchProcessTester(s => s
			.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
			.Java(j => j.JavaHomeMachine(DefaultJavaHome))
			.ConsoleSession(ConsoleSession.StartedSession)
			.FileSystem(s.FileSystemDefaults)
			.ProcessArguments(args)
		);

		public static ElasticsearchProcessTester AllDefaults(ConsoleSession session, bool interactive = true) => new ElasticsearchProcessTester(s => s
			.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
			.Java(j => j.JavaHomeMachine(DefaultJavaHome))
			.ConsoleSession(session)
			.FileSystem(s.FileSystemDefaults)
			.Interactive(interactive)
		);

		public static ElasticsearchProcessTester JavaChangesOnly(
			Func<MockJavaEnvironmentStateProvider, MockJavaEnvironmentStateProvider> javaState) => new ElasticsearchProcessTester(s => s
			.Elasticsearch(e => e.EsHomeMachineVariable(DefaultEsHome))
			.Java(javaState)
			.ConsoleSession(ConsoleSession.StartedSession)
			.FileSystem(s.FileSystemDefaults)
		);

		public static ElasticsearchProcessTester ElasticsearchChangesOnly(
			Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> esState,
			params string[] args) => ElasticsearchChangesOnly(null, esState, args);

		public static ElasticsearchProcessTester ElasticsearchChangesOnly(
			string homeDirectoryForFileSystem,
			Func<MockElasticsearchEnvironmentStateProvider, MockElasticsearchEnvironmentStateProvider> esState,
			params string[] args) =>
			new ElasticsearchProcessTester(s => s
				.Elasticsearch(esState)
				.Java(j => j.JavaHomeMachine(DefaultJavaHome))
				.ConsoleSession(ConsoleSession.StartedSession)
				.FileSystem(fs=> s.AddJavaExe(s.AddElasticsearchLibs(fs, homeDirectoryForFileSystem)))
				.ProcessArguments(args)
			);

		public static void CreateThrows(
			Func<ElasticsearchProcessTesterStateProvider, ElasticsearchProcessTesterStateProvider> setup,
			Action<Exception> assert)
		{
			var created = false;
			try
			{
				var elasticsearchProcessTester = new ElasticsearchProcessTester(setup);
				created = true;
			}
			catch (Exception e)
			{
				assert(e);
			}
			if (created)
				throw new Exception("Process tester expected elasticsearch.exe to throw an exception");
		}
		
		public void Start(Action<ElasticsearchProcessTester> assert)
		{
			this.Process.Start();
			assert(this);
		}

		public void RunToCompletion(Action<ElasticsearchProcessTester> assert)
		{
			this.Process.Start();
			if (this.Process.CompletedHandle.WaitOne(TimeSpan.FromSeconds(0.5)))
				assert(this);
			else
				throw new Exception("Could not run process to completion");
		}

		public void StartThrows(Action<Exception, ElasticsearchProcessTester> assert)
		{
			var started = false;
			try
			{
				this.Process.Start();
				started = true;
			}
			catch (Exception e)
			{
				assert(e, this);
			}
			if (started)
				throw new Exception("Process tester expected elasticsearch.exe to throw an exception");
		}
	}
}