using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.UI.Controls;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	/// <summary>
	/// Interaction logic for ConfigurationText.xaml
	/// </summary>
	public partial class ConfigurationView : StepControl<ConfigurationModel, ConfigurationView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(nameof(ViewModel), typeof(ConfigurationModel), typeof(ConfigurationView), new PropertyMetadata(null, ViewModelPassed));

		public override ConfigurationModel ViewModel
		{
			get => (ConfigurationModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}

		private readonly Brush _defaultBrush;
		private readonly Brush _defaultUpDownBrush;
		public ConfigurationView()
		{
			InitializeComponent();
			this._defaultBrush = this.NodeNameTextBox.BorderBrush;
			this._defaultUpDownBrush = this.MinimumMasterTextBox.Foreground;
		}

		protected override void InitializeBindings()
		{
			this.ViewModel.AddUnicastNodeUITask = () =>
			{
				var metroWindow = (Application.Current.MainWindow as MetroWindow);
				return metroWindow.ShowInputAsync(
					ViewResources.ConfigurationView_AddUnicastNode_Title,
					ViewResources.ConfigurationView_AddUnicastNode_Message);
			};

			this.AddUnicastNodeButton.Command = this.ViewModel.AddUnicastNode;
			this.RemoveUnicastNodeButton.Command = this.ViewModel.RemoveUnicastNode;

			this.HttpPortTextBox.Minimum = ConfigurationModel.HttpPortMinimum;
			this.HttpPortTextBox.Maximum = ConfigurationModel.PortMaximum;
			this.TransportPortTextBox.Minimum = ConfigurationModel.TransportPortMinimum;
			this.TransportPortTextBox.Maximum = ConfigurationModel.PortMaximum;

			this.OneWayBind(ViewModel, vm => vm.UnicastNodes, v => v.UnicastNodesListBox.ItemsSource);
			this.Bind(ViewModel, vm => vm.SelectedUnicastNode, v => v.UnicastNodesListBox.SelectedItem);
			this.Bind(ViewModel, vm => vm.ClusterName, v => v.ClusterNameTextBox.Text);
			this.Bind(ViewModel, vm => vm.NodeName, v => v.NodeNameTextBox.Text);
			this.Bind(ViewModel, vm => vm.NetworkHost, v => v.NetworkHostTextBox.Text);
			this.Bind(ViewModel, vm => vm.MasterNode, v => v.MasterNodeCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.DataNode, v => v.DataNodeCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.IngestNode, v => v.IngestNodeCheckBox.IsChecked);

			this.OneWayBind(ViewModel, vm => vm.SelectedMemory, v => v.MbLabel.Content, m => $"{FormatMb(m)}/{FormatMb(this.ViewModel.TotalPhysicalMemory)}");
			this.OneWayBind(ViewModel, vm => vm.MaxSelectedMemory, v => v.MemorySlider.Maximum);
			this.OneWayBind(ViewModel, vm => vm.MinSelectedMemory, v => v.MemorySlider.Minimum);
			this.Bind(ViewModel, vm => vm.SelectedMemory, v => v.MemorySlider.Value);
			this.Bind(ViewModel, vm => vm.LockMemory, v => v.LockMemoryCheckBox.IsChecked);
			this.Bind(ViewModel, vm => vm.MinimumMasterNodes, v => v.MinimumMasterTextBox.Value, null, new IntToDoubleConverter(), new NullableDoubleToIntConverter());
			this.Bind(ViewModel, vm => vm.HttpPort, v => v.HttpPortTextBox.Value, null, new NullableIntToNullableDoubleConverter(), new NullableDoubleToNullableIntConverter());
			this.Bind(ViewModel, vm => vm.TransportPort, v => v.TransportPortTextBox.Value, null, new NullableIntToNullableDoubleConverter(), new NullableDoubleToNullableIntConverter());

			this.WhenAnyValue(v => v.ViewModel.MinimumMasterNodes).Subscribe(OnMinimumMasterNodesChange);
		}


		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> failures)
		{
			var b = isValid ? this._defaultBrush : new SolidColorBrush(Color.FromRgb(233,73,152));
			this.NodeNameTextBox.BorderBrush = _defaultBrush;
			this.ClusterNameTextBox.BorderBrush = _defaultBrush;
			if (isValid) return;
			foreach (var e in this.ViewModel.ValidationFailures)
			{
				switch (e.PropertyName)
				{
					case "ClusterName":
						this.ClusterNameTextBox.BorderBrush = b;
						continue;
					case "NodeName":
						this.NodeNameTextBox.BorderBrush = b;
						continue;
				}
			}
		}

		private static string FormatMb(ulong megabytes)
		{
			const int scale = 1024;
			var orders = new [] { "GB", "MB" };
			var max = (ulong)Math.Pow(scale, orders.Length - 1);

			foreach (var order in orders)
			{
				if (megabytes > max)
					return $"{decimal.Divide(megabytes, max):##.##} {order}";

				max /= scale;
			}
			return "0Mb";
		}

		private void OnMinimumMasterNodesChange(int i)
		{
			if (i == 0)
			{
				this.MinimumMasterTextBox.StringFormat = ViewResources.ConfigurationView_MinimumMasterNodesNotSet;
				this.MinimumMasterTextBox.FontWeight = FontWeights.Normal;
				this.MinimumMasterTextBox.Foreground = this._defaultBrush;
			}
			else
			{
				this.MinimumMasterTextBox.StringFormat = ViewResources.ConfigurationView_MinimumMasterNodesSet;
				this.MinimumMasterTextBox.FontWeight = FontWeights.Bold;
				this.MinimumMasterTextBox.Foreground = this._defaultUpDownBrush;
			}
		}
	}
}
