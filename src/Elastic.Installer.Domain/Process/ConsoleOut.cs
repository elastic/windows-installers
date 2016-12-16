namespace Elastic.Installer.Domain.Process.ObservableWrapper
{
	public class ConsoleOut
	{
		public bool Error { get; }
		public string Data { get; }
		protected ConsoleOut(bool error, string data)
		{
			this.Error = error;
			this.Data = data;
		}

		public static ConsoleOut ErrorOut(string data) => new ConsoleOut(true, data);
		public static ConsoleOut Out(string data) => new ConsoleOut(false, data);
	}
}