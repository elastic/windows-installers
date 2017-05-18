using System;
using Elastic.Installer.Domain.Process;

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
			if (this._interactive && ElasticsearchConsoleOutParser.TryParse(c,
				    out string date, out string level, out string section, out string node, out string message, out bool started))
			{
				var writer = c.Error ? Console.Error : Console.Out;
				Console.ForegroundColor = ConsoleColor.DarkGray;
				writer.Write($"[{date}]");
				switch (level ?? "")
				{
					case "WARN":
						Console.ForegroundColor = ConsoleColor.Yellow;
						break;
					case "FATAL":
					case "ERROR":
						Console.ForegroundColor = ConsoleColor.Red;
						break;
					case "DEBUG":
					case "TRACE":
						Console.ForegroundColor = ConsoleColor.DarkGray;
						break;
					default:
						Console.ForegroundColor = ConsoleColor.DarkGreen;
						break;
				}
				writer.Write($"[{level?.PadRight(5)}]");
				Console.ForegroundColor = ConsoleColor.DarkMagenta;
				writer.Write($"[{section?.PadRight(26)}] ");
				Console.ForegroundColor = ConsoleColor.Cyan;
				writer.Write($"[{node}] ");
				Console.ForegroundColor = ConsoleColor.White;
				writer.Write(message + Environment.NewLine);
				writer.Flush();
				Console.ResetColor();
			}

			base.Write(c);

		}
	}
}