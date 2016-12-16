namespace Elastic.Installer.UI.Progress
{
	public class ProgressIndicator
	{
		public double Progress { get; }

		public string Message { get; }

		public ProgressIndicator(double progress, string message = null)
		{
			Progress = progress;
			Message = message;
		}
	}
}