using System;

namespace Elastic.Installer.Domain.Process
{
	public class StartupException : Exception
	{
		public StartupException(string message) : base(message)
		{
		}
	}
}