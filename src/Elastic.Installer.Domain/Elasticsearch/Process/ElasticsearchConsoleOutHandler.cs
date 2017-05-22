using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Elastic.Installer.Domain.Process;
using static System.ConsoleColor;

namespace Elastic.Installer.Domain.Elasticsearch.Process
{
	public class ElasticsearchConsoleOutHandler : ConsoleOutHandler
	{
		private readonly bool _interactive;

		public ElasticsearchConsoleOutHandler(bool interactive)
		{
			this._interactive = interactive;
		}

		public override void Write(ConsoleOut c)
		{
			var w = c.Error ? Console.Error : Console.Out;
			if (this._interactive && ElasticsearchConsoleOutParser.TryParse(c,
				    out string date, out string level, out string section, out string node, out string message, out bool started))
			{
				WriteBlock(w, DarkGray, date);
				WriteBlock(w, LevelColor(level), level, 5);

				if (!string.IsNullOrWhiteSpace(section))
				{
					WriteBlock(w, DarkCyan, section, 25);
					WriteSpace(w);
				}
				WriteBlock(w, DarkGreen, node);
				WriteSpace(w);

				var messageColor = c.Error || level == "ERROR" ? Red : White;
				WriteMessage(w, messageColor, message);
			}

			else if (c.Error)
			{
				Console.ForegroundColor = Red;
				w.WriteLine(c.Data);
			}
			else
				w.WriteLine(c.Data);
			Console.ResetColor();
			w.Flush();
		}

		private static ConsoleColor LevelColor(string level)
		{
			switch (level ?? "")
			{
				case "WARN": return Yellow;
				case "FATAL":
				case "ERROR":
					return Red;
				case "DEBUG":
				case "TRACE":
					return DarkGray;
				default:
					return Cyan;
			}
		}

		private static readonly char[] _anchors = {'[', ']'};
		private static IEnumerable<string> Parts(string s)
		{
			int start = 0, index;
			while ((index = s.IndexOfAny(_anchors, start)) != -1)
			{
				if(index-start > 0)
					yield return s.Substring(start, index - start);

				yield return s.Substring(index, 1);
				start = index + 1;
			}
			if (start < s.Length)
				yield return s.Substring(start);
		}

		private static void WriteMessage(TextWriter w, ConsoleColor color, string message)
		{
			var inside = 0;
			foreach (var p in Parts(message))
			{
				if (p.Length == 0) continue;
				if (p[0] == '[') inside++;
				if (p[0] == ']') inside--;
				Console.ForegroundColor =
					(p[0] == '[' && inside > 1)
				    || (p[0] != '[' && inside > 0)? Yellow : color;
				w.Write(p);
			}
			Console.ResetColor();
			w.WriteLine();

		}


		private static void WriteSpace(TextWriter w) => w.Write(" ");
		private static void WriteBlock(TextWriter w, ConsoleColor color, string block, int? pad = null)
		{
			var b = pad != null ? block.PadRight(pad.Value) : block;
			Console.ForegroundColor = DarkGray;
			w.Write("[");
			Console.ForegroundColor = color;
			w.Write(b);
			Console.ForegroundColor = DarkGray;
			w.Write("]");

		}
	}
}