using System;

namespace Elastic.ProcessHosts.Process
{
	public class StartupException : Exception
	{
		public StartupException(string message) : base(message)
		{
		}
	}
}