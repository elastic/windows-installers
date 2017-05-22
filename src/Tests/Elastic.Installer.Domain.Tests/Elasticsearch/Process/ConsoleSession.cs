using System.Collections.Generic;
using Elastic.Installer.Domain.Process;

namespace Elastic.Installer.Domain.Tests.Elasticsearch.Process
{
	public class ConsoleSession : List<ConsoleOut>
	{
		public ConsoleSession() { }
		public ConsoleSession(ConsoleSession session)
		{
			this.AddRange(session);
		}


		public void Add(string o)
		{
			this.Add(ConsoleOut.Out(o));

		}
		public void Add(char x, string o)
		{
			this.Add(ConsoleOut.Out(o));
		}

		public static ConsoleSession BeforeStartedSession => new ConsoleSession
		{
			{"[x][INFO ][o.e.e.NodeEnvironment    ] [N] using [1] data paths, mounts [[BOOTCAMP (C:)]], net usable_space [27.7gb], net total_space [129.7gb], spins? [unknown], types [NTFS]"},
			{"[x][INFO ][o.e.e.NodeEnvironment    ] [N] heap size [1.9gb], compressed ordinary object pointers [true]"},
			{"[x][INFO ][o.e.n.Node               ] [N] version[5.0.0-beta1], pid[13172], build[7eb6260/2016-09-20T23:10:37.942Z], OS[Windows 10/10.0/amd64], JVM[Oracle Corporation/Java HotSpot(TM) 64-Bit Server VM/1.8.0_101/25.101-b13]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [aggs-matrix-stats]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [ingest-common]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [lang-expression]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [lang-groovy]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [lang-mustache]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [lang-painless]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [percolator]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [reindex]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [transport-netty3]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded module [transport-netty4]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded plugin [ingest-attachment]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded plugin [ingest-geoip]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded plugin [mapper-attachments]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded plugin [mapper-murmur3]"},
			{"[x][INFO ][o.e.p.PluginsService     ] [N] loaded plugin [x-pack]"},
			{"[x][WARN ][d.m.attachment           ] [N] plugin has been deprecated and will be replaced by [ingest-attachment] plugin."},
			{"[x][INFO ][o.e.n.Node               ] [N] initialized"},
			{"[x][INFO ][o.e.n.Node               ] [N] starting ..."},
		};
		public static ConsoleSession StartedSession => new ConsoleSession(BeforeStartedSession)
		{
			{"[x][INFO ][o.e.n.Node               ] [N] started"},
		};
	}
}