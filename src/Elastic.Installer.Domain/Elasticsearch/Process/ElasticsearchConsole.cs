using Elastic.Installer.Domain.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elastic.Installer.Domain
{
	public static class ElasticsearchConsole
	{
		public static void WriteLine(ConsoleColor foregroundColor, string value)
		{
			ConsoleColor originalColor = Console.ForegroundColor;
			Console.ForegroundColor = foregroundColor;

			Console.WriteLine(value);
			Console.Out.Flush();

			Console.ForegroundColor = originalColor;
		}

		public static void WriteLine(string value)
		{
			Console.WriteLine(value);
			Console.Out.Flush();
		}


		public static void WriteLine(string date, string level, string section, string node, string message)
		{
			ConsoleColor originalColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.DarkGray;
			Console.Write($"[{date}]");
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
			Console.Write($"[{level?.PadRight(5)}]");
			Console.ForegroundColor = ConsoleColor.DarkMagenta;
			Console.Write($"[{section?.PadRight(26)}] ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write($"[{node}] ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(message + Environment.NewLine);
			Console.Out.Flush();
			Console.ForegroundColor = originalColor;
		}
	}
}
