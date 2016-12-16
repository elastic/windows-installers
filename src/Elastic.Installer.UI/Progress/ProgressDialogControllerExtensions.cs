using MahApps.Metro.Controls.Dialogs;

namespace Elastic.Installer.UI.Progress
{
	public static class ProgressDialogControllerExtensions
	{
		public static void UpdateProgress(this ProgressDialogController controller, ProgressIndicator indicator)
		{
			if (indicator != null)
			{
				controller.SetProgress(indicator.Progress);
				controller.SetMessage(indicator.Message);
			}
		}
	}
}