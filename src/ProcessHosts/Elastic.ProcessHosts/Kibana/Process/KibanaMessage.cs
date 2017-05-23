using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Elastic.ProcessHosts.Process;

namespace Elastic.ProcessHosts.Kibana.Process
{
	public class KibanaMessage : ConsoleOut
	{
		private static readonly Regex StartedMessage =
			new Regex(@"Server running at https?:\/\/(?<host>[^:]+)(:(?<port>\d*))?");

		public DateTime Date { get; }

		public string Type { get; }

		public string Message { get; }

		public string State { get; }

		public int ProcessId { get; }

		public IEnumerable<string> Tags { get; }

		public KibanaMessage(string consoleLine) : base(false, consoleLine)
		{
			if (string.IsNullOrEmpty(consoleLine)) return;
			throw new NotImplementedException("Parking this for now");

			try
			{
//				//var message = JsonConvert.DeserializeObject<KibanaLogMessage>(consoleLine);
//				Type = message.Type;
//				Date = message.Timestamp;
//				Tags = message.Tags;
//				ProcessId = message.ProcessId;
//				State = message.State;
//				Message = message.Message;
			}
			catch (Exception)
			{
				throw new Exception($"Cannot deserialize ${consoleLine}");
			}
		}

		public bool TryGetStartedConfirmation(out string host, out int? port)
		{
			var match = StartedMessage.Match(this.Message);
			host = null;
			port = null;

			if (!match.Success) return false;

			host = match.Groups["host"].Value;
			var portValue = match.Groups["port"].Value;

			if (!string.IsNullOrEmpty(portValue))
				port = int.Parse(portValue);

			return true;
		}

//		public class KibanaLogMessage
//		{
//			[JsonProperty("type")]
//			public string Type { get; set; }
//
//			[JsonProperty("@timestamp")]
//			public DateTime Timestamp { get; set; }
//
//			[JsonProperty("tags")]
//			public IEnumerable<string> Tags { get; set; }
//
//			[JsonProperty("pid")]
//			public int ProcessId { get; set; }
//
//			[JsonProperty("state")]
//			public string State { get; set; }
//
//			[JsonProperty("message")]
//			public string Message { get; set; }
//		}
	}
}