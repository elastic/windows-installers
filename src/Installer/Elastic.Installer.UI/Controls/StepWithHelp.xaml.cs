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

namespace Elastic.Installer.UI.Controls
{
	/// <summary>
	/// Interaction logic for StepWithHelp.xaml
	/// </summary>
	public partial class StepWithHelp : UserControl
	{
		public static readonly DependencyProperty StepProperty =
				DependencyProperty.Register("Step", typeof(object), typeof(StepWithHelp), new UIPropertyMetadata(null));

		public static readonly DependencyProperty HelpTextProperty =
				DependencyProperty.Register("HelpText", typeof(string), typeof(StepWithHelp), new FrameworkPropertyMetadata());
		
		public static readonly DependencyProperty IsOpenProperty =
				DependencyProperty.Register("IsOpen", typeof(bool), typeof(StepWithHelp), new UIPropertyMetadata());

		public bool IsOpen
		{
			get { return (bool)GetValue(IsOpenProperty); }
			set { SetValue(IsOpenProperty , value); }
		}

		public object Step
		{
			get { return (object)GetValue(StepProperty); }
			set { SetValue(StepProperty, value); }
		}

		public string HelpText
		{
			get { return (string)GetValue(HelpTextProperty); }
			set { SetValue(HelpTextProperty, value); }
		}
		public StepWithHelp()
		{
			InitializeComponent();
		}

		public void ToggleHelp()
		{
			this.IsOpen = !this.IsOpen;
		}
	}
}
