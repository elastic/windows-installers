using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Elastic.Installer.UI.Shared
{
	/// <summary>
	/// Interaction logic for LicenseDialog.xaml
	/// </summary>
	public partial class LicenseDialog : UserControl
	{
		public LicenseDialog()
		{
			InitializeComponent();
		}
		public LicenseDialog(string content)
		{
			InitializeComponent();
			this.LicenseInformation.Text = content;
		}
	}
}
