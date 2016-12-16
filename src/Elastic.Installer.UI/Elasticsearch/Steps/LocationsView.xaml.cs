using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using Elastic.Installer.UI.Controls;
using FluentValidation.Results;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;

namespace Elastic.Installer.UI.Elasticsearch.Steps
{
	public partial class LocationsView : StepControl<LocationsModel, LocationsView>
	{
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register("ViewModel", typeof(LocationsModel), typeof(LocationsView), new PropertyMetadata(null, ViewModelPassed));

		public override LocationsModel ViewModel
		{
			get { return (LocationsModel)GetValue(ViewModelProperty); }
			set { SetValue(ViewModelProperty, value); }
		}

		private readonly Brush _defaultBrush;

		public LocationsView()
		{
			InitializeComponent();
			this._defaultBrush = this.InstallDirectoryTextBox.BorderBrush;
		}

		protected override void InitializeBindings()
		{
			this.Bind(ViewModel, vm => vm.ConfigureLocations, v => v.CustomLocationsRadioButton.IsChecked);
			this.Bind(ViewModel, vm => vm.PlaceWritableLocationsInSamePath, v => v.CustomLocationsCheckBox.IsChecked);

			this.WhenAnyValue(view => view.ViewModel.ConfigureLocations)
				.Subscribe(x =>
				{
					this.DefaultLocationsRadioButton.IsChecked = !x;
					//this.InstallDirectoryLabel.IsEnabled = x;
					this.InstallDirectoryTextBox.IsEnabled = x;
					this.InstallDirectoryBrowseButton.IsEnabled = x;
					this.CustomLocationsCheckBox.IsEnabled = x;
				});

			this.WhenAnyValue(view => view.ViewModel.ConfigureAllLocations)
				.Subscribe(x =>
				{
					this.DataDirectoryLabel.IsEnabled = x;
					this.DataDirectoryTextBox.IsEnabled = x;
					this.DataDirectoryBrowseButton.IsEnabled = x;
					this.ConfigDirectoryLabel.IsEnabled = x;
					this.ConfigDirectoryTextBox.IsEnabled = x;
					this.ConfigDirectoryBrowseButton.IsEnabled = x;
					this.LogsDirectoryLabel.IsEnabled = x;
					this.LogsDirectoryTextBox.IsEnabled = x;
					this.LogsDirectoryBrowseButton.IsEnabled = x;
				});

			this.Bind(ViewModel, vm => vm.InstallDir, v => v.InstallDirectoryTextBox.Text);
			this.Bind(ViewModel, vm => vm.DataDirectory, v => v.DataDirectoryTextBox.Text);
			this.Bind(ViewModel, vm => vm.ConfigDirectory, v => v.ConfigDirectoryTextBox.Text);
			this.Bind(ViewModel, vm => vm.LogsDirectory, v => v.LogsDirectoryTextBox.Text);

			this.InstallDirectoryTextBox.Events().TextChanged
				.Subscribe(e =>
				{
					if (ViewModel.PlaceWritableLocationsInSamePath && this.CustomLocationsCheckBox.IsChecked.HasValue)
						ViewModel.PlaceWritableLocationsInSamePath = this.CustomLocationsCheckBox.IsChecked.Value;
				});
			this.CustomLocationsCheckBox.Events().Click
				.Subscribe(e => ViewModel.PlaceWritableLocationsInSamePath = this.CustomLocationsCheckBox.IsChecked.GetValueOrDefault());
			this.InstallDirectoryBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFolder(ViewModel.InstallDir, (result) => { ViewModel.InstallDir = result; }));
			this.DataDirectoryBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFolder(ViewModel.DataDirectory, (result) => { ViewModel.DataDirectory = result; }));
			this.ConfigDirectoryBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFolder(ViewModel.ConfigDirectory, (result) => { ViewModel.ConfigDirectory = result; }));
			this.LogsDirectoryBrowseButton.Events().Click
				.Subscribe(folder => this.BrowseForFolder(ViewModel.LogsDirectory, (result) => { ViewModel.LogsDirectory = result; }));
			this.DefaultLocationsRadioButton.Events().Click
				.Subscribe(e => ViewModel.SetDefaultLocations());
		}

		protected void BrowseForFolder(string defaultLocation, Action<string> setter)
		{

			var dlg = new CommonOpenFileDialog();
			dlg.IsFolderPicker = true;
			dlg.InitialDirectory = defaultLocation;

			dlg.AddToMostRecentlyUsedList = false;
			dlg.AllowNonFileSystemItems = false;
			dlg.DefaultDirectory = defaultLocation;
			dlg.EnsureFileExists = true;
			dlg.EnsurePathExists = true;
			dlg.EnsureReadOnly = false;
			dlg.EnsureValidNames = true;
			dlg.Multiselect = false;
			dlg.ShowPlacesList = true;
			var result = dlg.ShowDialog();
			if (result == CommonFileDialogResult.Ok) setter(dlg.FileName);
		}

		protected override void UpdateValidState(bool isValid, IList<ValidationFailure> validationFailures)
		{
			var b = isValid ? this._defaultBrush : new SolidColorBrush(Color.FromRgb(233,73,152));
			this.InstallDirectoryTextBox.BorderBrush = _defaultBrush;
			this.DataDirectoryTextBox.BorderBrush = _defaultBrush;
			this.ConfigDirectoryTextBox.BorderBrush = _defaultBrush;
			this.LogsDirectoryTextBox.BorderBrush = _defaultBrush;
			if (isValid) return;
			foreach (var e in this.ViewModel.ValidationFailures)
			{
				switch (e.PropertyName)
				{
					case nameof(this.ViewModel.InstallDir):
						this.InstallDirectoryTextBox.BorderBrush = b;
						continue;
					case nameof(this.ViewModel.DataDirectory):
						this.DataDirectoryTextBox.BorderBrush = b;
						continue;
					case nameof(this.ViewModel.LogsDirectory):
						this.LogsDirectoryTextBox.BorderBrush = b;
						continue;
					case nameof(this.ViewModel.ConfigDirectory):
						this.ConfigDirectoryTextBox.BorderBrush = b;
						continue;
				}
			}
		}
	}
}
