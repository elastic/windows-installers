using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Elastic.Installer.UI.Controls;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Elastic.Installer.UI.Properties;
using FluentValidation.Results;
using System.Threading;
using System.Reactive.Disposables;
using System.Reflection;
using Elastic.Installer.UI.Progress;
using Microsoft.Deployment.WindowsInstaller;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Elastic.Configuration.EnvironmentBased;
using Elastic.Configuration.EnvironmentBased.Java;
using Elastic.Configuration.FileBased.Yaml;
using Elastic.Installer.Domain.Configuration.Wix.Session;
using Elastic.Installer.Domain.Extensions;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Base.Closing;
using Elastic.Installer.Domain.Model.Base.Service;
using Elastic.Installer.Domain.Model.Elasticsearch;
using Elastic.Installer.Domain.Model.Elasticsearch.Closing;
using Elastic.Installer.Domain.Model.Elasticsearch.Config;
using Elastic.Installer.Domain.Model.Elasticsearch.Locations;
using Elastic.Installer.Domain.Model.Elasticsearch.Notice;
using Elastic.Installer.Domain.Model.Elasticsearch.Plugins;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using Elastic.Installer.Domain.Model.Shared;
using Elastic.Installer.UI.Elasticsearch.Steps;
using Elastic.Installer.UI.Shared;
using Elastic.Installer.UI.Shared.Steps;
using FluentValidation.Internal;
using Semver;

namespace Elastic.Installer.UI.Elasticsearch
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow, IViewFor<ElasticsearchInstallationModel>, IEmbeddedWindow
	{
		private readonly ManualResetEvent _installerStartEvent;
		private readonly ISession _session;	
		private readonly string _currentVersion;
		private readonly IDictionary<Type, Action<IValidatableReactiveObject, StepWithHelp>> _stepModelToControl;
		private readonly IDictionary<Type, MetroTabItem> _cachedTabs = new Dictionary<Type, MetroTabItem>();
		private readonly SolidColorBrush _defaultTabForeground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
		private readonly SolidColorBrush _hoverTabForeground = new SolidColorBrush(Color.FromRgb(0, 169, 224));
		private readonly SolidColorBrush _disabledTabForeground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
		private readonly SolidColorBrush _selectedTabForeground = new SolidColorBrush(Color.FromRgb(0, 169, 224));
		private readonly SolidColorBrush _badTabForeground = new SolidColorBrush(Color.FromRgb(233, 73, 152));
		private ProgressDialogController _controller;
		private InstallProgressCounter _progressCounter;

		object IViewFor.ViewModel
		{
			get => ViewModel;
			set => ViewModel = (ElasticsearchInstallationModel)value;
		}

		public ElasticsearchInstallationModel ViewModel { get; set; }

		public MainWindow(ElasticsearchInstallationModel setupViewModel, ManualResetEvent startEvent)
		{
			this._installerStartEvent = startEvent;
			this._session = setupViewModel.Session;
			this._currentVersion = _session.Get<string>("CurrentVersion");
			var semanticVersion = SemVersion.Parse(this._currentVersion);

			_stepModelToControl = new Dictionary<Type, Action<IValidatableReactiveObject, StepWithHelp>>
			{
				{
					typeof(NoticeModel), (m, s) => Step(s, new NoticeView { ViewModel = m as NoticeModel },
						ViewResources.MainWindow_Help_Header, string.Format(ViewResources.MainWindow_Help, "Elasticsearch")) },
				{
					typeof(LocationsModel), (m, s) => Step(s, new LocationsView { ViewModel = m as LocationsModel },
						ViewResources.LocationsView_Elasticsearch_Help_Header, ViewResources.LocationsView_Elasticsearch_Help)  },
				{
					typeof(ServiceModel), (m, s) => Step(s, new ServiceView { ViewModel = m as ServiceModel },
						ViewResources.ServiceView_Elasticsearch_Help_Header, ViewResources.ServiceView_Elasticsearch_Help) },
				{
					typeof(ConfigurationModel), (m, s) => Step(s, new ConfigurationView { ViewModel = m as ConfigurationModel },
						ViewResources.ConfigurationView_Elasticsearch_Help_Header, ViewResources.ConfigurationView_Elasticsearch_Help) },
				{
					typeof(PluginsModel), (m, s) => Step(s, new PluginsView { ViewModel = m as PluginsModel },
						ViewResources.PluginsView_Elasticsearch_Help_Header, ViewResources.PluginsView_Elasticsearch_Help) },
				{
					typeof(XPackModel), (m, s) => Step(s, new XPackView { ViewModel = m as XPackModel },
						ViewResources.XPackView_Elasticsearch_Help_Header, string.Format(ViewResources.XPackView_Elasticsearch_Help, $"{semanticVersion.Major}.{semanticVersion.Minor}")) },
				{ typeof(ClosingModel), (m, s) => Step(s, new ClosingView { ViewModel = m as ClosingModel }, null, null) },
			};

			Application.Current.Resources["InstallerTitle"] = this._currentVersion;

			InitializeComponent();

			this.Title = string.Format(ViewResources.MainWindow_Title, _currentVersion);
			this.ViewModel = setupViewModel;
			this.DataContext = this.ViewModel;
			SetupViewModelCommands();
			TabNavigation();

			this.Bind(ViewModel, vm => vm.NextButtonText, view => view.NextButton.Content);

			this.WhenAny(view => view.ViewModel.FirstInvalidStepValidationFailures, v => v.GetValue().Count)
				.Subscribe(errorCount =>
				{
					if (errorCount == 0) this.ValidationErrorLink.Text = null;
					if (errorCount == 1) this.ValidationErrorLink.Text = $"1 {ViewResources.MainWindow_ValidationError}";
					if (errorCount > 1) this.ValidationErrorLink.Text = $"{errorCount} {ViewResources.MainWindow_ValidationErrors}";
				});

			this.WhenAnyValue(view => view.ViewModel.ClosingModel.Installed)
				.Subscribe(b =>
				{
					if (b.HasValue) this.AnimateLogo();
				});
		}

		public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord, MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
		{
			if (_controller.IsCanceled)
			{
				this.ViewModel.ClosingModel.Installed = ClosingResult.Cancelled;
				return MessageResult.Cancel;
			}

			var indicator = _progressCounter.ProcessMessage(messageType, messageRecord);
			_controller.UpdateProgress(indicator);

			// Have we finished?
			if (messageType == InstallMessage.InstallEnd)
			{
				try
				{
					var returnCode = messageRecord.GetInteger(3);
					this.ViewModel.ClosingModel.Installed = returnCode == 1 ? ClosingResult.Success : ClosingResult.Failed;
				}
				catch (Exception)
				{
					this.ViewModel.ClosingModel.Installed = ClosingResult.Failed;
				}
			}

			return MessageResult.OK;
		}

		private static void Step(StepWithHelp step, UserControl view, string helpHeaderText, string help)
		{
			step.Step = view;
			step.HelpText = help;
			step.HelpHeaderText = helpHeaderText;
		}

		private void TabNavigation()
		{
			DrawTabs();
			this.ViewModel.Steps.Changed.Subscribe(e => DrawTabs());
			
			this.Bind(ViewModel, vm => vm.TabSelectedIndex, view => view.StepsTab.SelectedIndex);

			this.WhenAnyValue(view => view.ViewModel.TabSelectedIndex, view => view.ViewModel.TabSelectionMax)
				.Subscribe(t => RedrawTabColors(selected: t.Item1, max: t.Item2));
		}

		private void RedrawTabColors(int selected, int max)
		{
			var i = 0;
			foreach (var step in this.ViewModel.Steps)
			{
				if (!this.RedrawTabColor(selected, max, step, i)) continue;
				i++;
			}
		}

		private bool RedrawTabColor(int selected, int max, IStep step, int i)
		{
			if (!this._cachedTabs.TryGetValue(step.GetType(), out var child)) return false;

			var label = child.FindChildren<Label>().First();
			child.IsEnabled = true;
			label.IsEnabled = true;

			label.Foreground = _defaultTabForeground;
			if (i == selected)
				label.Foreground = _selectedTabForeground;

			if (!step.IsValid)
				label.Foreground = _badTabForeground;

			if (i > max)
			{
				child.IsEnabled = false;
				label.IsEnabled = false;
				label.Foreground = _disabledTabForeground;
			}
			if (selected >= this.ViewModel.Steps.Count - 1)
				child.Visibility = Visibility.Collapsed;
			return true;
		}

		private void DrawTabs()
		{
			var i = 0;
			var tabs = new List<MetroTabItem>();
			foreach (var step in this.ViewModel.Steps)
			{
				var tab = CreateTab(step, i);
				tabs.Add(tab);
				i++;
			}
			this.StepsTab.ItemsSource = tabs;
			this.RedrawTabColors(this.ViewModel.TabSelectedIndex, this.ViewModel.TabSelectionMax);
		}

		private MetroTabItem CreateTab(IStep m, int i)
		{
			var type = m.GetType();
			if (this._cachedTabs.TryGetValue(type, out var tabItem))
				return tabItem;
			
			var step = new StepWithHelp
			{
				Margin = new Thickness(0, 10, 0, 0),
				VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalAlignment = HorizontalAlignment.Stretch,
			};
			_stepModelToControl[type](m, step);

			var model = this.ViewModel;
			void Reset() => RedrawTabColor(model.TabSelectedIndex, model.TabSelectionMax, m, i);
			var headerLabel = new Label
			{
				Content = m.Header,
				FontSize = 26,
				Margin = new Thickness(-4, 0, 0, 0)
			};

			headerLabel.MouseLeave += (ss, e) => Reset();
			headerLabel.MouseLeftButtonDown += (ss, e) => Reset();
			headerLabel.MouseEnter += (ss, e) =>
			{
				if (model.TabSelectedIndex == i) return;
				headerLabel.Cursor = Cursors.Hand;
				headerLabel.Foreground = this._hoverTabForeground;
			};

			var tab = new MetroTabItem {Content = step, Header = headerLabel};
			this._cachedTabs.Add(type, tab);
			return tab;
		}

		private void SetupViewModelCommands()
		{
			this.ViewModel.InstallUITask = async () =>
			{
				JumpToInstallationResult();
				return await InstallAsync();
			};

			this.ViewModel.Help.Subscribe(x =>
			{
				var tabItem = this.StepsTab.Items[this.ViewModel.TabSelectedIndex] as TabItem;
				var stepWithHelp = tabItem?.Content as StepWithHelp;
				if (stepWithHelp == null) return;
				this.HelpHtmlControl.Html = stepWithHelp.HelpText;
				this.FlyoutControl.Header = stepWithHelp.HelpHeaderText;
				this.FlyoutControl.IsOpen = !this.FlyoutControl.IsOpen;
			});

			this.ViewModel.ShowLicenseBlurb.Subscribe(async x =>
			{
				await ShowLicenseDialog();

				//await this.ShowMessageAsync(
				//	ViewResources.MainWindow_LicenseHeader,
				//	ViewResources.MainWindow_LicenseInformation,
				//	MessageDialogStyle.Affirmative,
				//	new MetroDialogSettings()).ConfigureAwait(true);
			});

			this.ViewModel.ShowCurrentStepErrors.Subscribe(async x =>
			{
				if (this.ViewModel.TabFirstInvalidIndex.HasValue)
					this.ViewModel.TabSelectedIndex = this.ViewModel.TabFirstInvalidIndex.Value;
				var message = string.Join(Environment.NewLine, this.ViewModel.FirstInvalidStepValidationFailures.Select(v => v.ErrorMessage.ValidationMessage()));
				await this.ShowMessageAsync(
					ViewResources.MainWindow_ValidationErrors,
					message,
					MessageDialogStyle.Affirmative,
					new MetroDialogSettings()).ConfigureAwait(true);
				
			});
			this.ViewModel.Exit.Subscribe(x =>
			{
				if (this.ViewModel.ClosingModel.OpenDocumentationAfterInstallation)
					Process.Start(ViewResources.MainWindow_DocumentationLink);
				this.Close();
			});

			this.WhenAnyValue(view => view.ViewModel.NextButtonText)
				.Subscribe(buttonText =>
				{
					if (string.Equals(buttonText, ViewResources.SetupView_NextText, StringComparison.InvariantCultureIgnoreCase))
						this.NextButton.Command = this.ViewModel.Next;
					else if (string.Equals(buttonText, ViewResources.SetupView_ExitText, StringComparison.InvariantCultureIgnoreCase))
					{
						this.NextButton.Command = this.ViewModel.Exit;
						this.HelpButton.Visibility = Visibility.Hidden;
						this.BackButton.Visibility = Visibility.Hidden;
						this.RefreshButton.Visibility = Visibility.Hidden;
					}
					else this.NextButton.Command = this.ViewModel.Install;
				});

			this.WhenAnyValue(view => view.ViewModel.PluginsModel.HttpsProxyHost, view => view.ViewModel.PluginsModel.HttpsProxyPort)
				.Subscribe(hostAndPort =>
				{
					ProxyLabel.Content = hostAndPort.Item1 == null 
						? null 
						: $"Configured HTTPS proxy: {ViewModel.PluginsModel.HttpsProxyHostAndPort}";
				});

			this.WhenAnyValue(view => view.ViewModel.ProxyButtonVisible)
				.Subscribe(visible =>
				{
					var visibility = visible ? Visibility.Visible : Visibility.Hidden;
					ProxyButton.Visibility = visibility;
					ProxyLabel.Visibility = visibility;
				});
		}

		private void JumpToInstallationResult()
		{
			//disable all tabs, whether the installer succeeds or fails the only allowed action afterwards is exiting.
			this.ViewModel.TabSelectedIndex = this.ViewModel.Steps.Count - 1;
		}

		private void PromptPrerequisiteFailures(IList<ValidationFailure> failures)
		{
			if (failures.Count == 0) return;

			this.JumpToInstallationResult();
			this.ViewModel.ClosingModel.Installed = ClosingResult.Preempted;
			this.ViewModel.ClosingModel.PrerequisiteFailures = this.ViewModel.PrerequisiteFailures;
			this.ViewModel.ClosingModel.PrerequisiteFailureMessages = this.ViewModel.PrerequisiteFailures.Select(v => v.ErrorMessage);
		}

		private async Task ShowLicenseDialog()
		{
			var customDialog = new CustomDialog { Title = ViewResources.MainWindow_LicenseHeader };
			var licenseModel = new LicenseModel();
			licenseModel.OpenLicense.Subscribe(x => 
				Process.Start(string.Format(ViewResources.MainWindow_LicenseLink, "elasticsearch", _currentVersion)));
			licenseModel.Close.Subscribe(async x => await this.HideMetroDialogAsync(customDialog));
			customDialog.Content = new LicenseDialog { DataContext = licenseModel };

			await this.ShowMetroDialogAsync(customDialog);
		}

		private async Task<IObservable<ClosingResult>> InstallAsync()
		{
			SetSessionValues(this.ViewModel);
			SetSessionValues(this.ViewModel.NoticeModel);
			SetSessionValues(this.ViewModel.LocationsModel);
			SetSessionValues(this.ViewModel.ConfigurationModel);
			SetSessionValues(this.ViewModel.ServiceModel);
			SetSessionValues(this.ViewModel.PluginsModel);
			SetSessionValues(this.ViewModel.XPackModel);

			this._progressCounter = new InstallProgressCounter();
			this.StepsTab.Visibility = Visibility.Hidden;
			this._controller = await this.ShowProgressAsync(
				ViewResources.MainWindow_InstallingTitle,
				string.Format(ViewResources.MainWindow_InstallingMessage, this._currentVersion),
				true);

			return Observable.Create<ClosingResult>(o =>
			{
				this._installerStartEvent.Set();
				return Disposable.Empty;
			});
		}

		private void SetSessionValues(object viewModel)
		{
			var type = viewModel.GetType();
			var ps = ElasticsearchArgumentParser.ArgumentsByModel[type];
			foreach (var p in ps)
			{
				var pi = type.GetProperty(p, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (pi != null)
				{
					var value = this.ViewModel.ParsedArguments.MsiString(pi.GetValue(viewModel, null));
					_session.Set(p, value);
				}
			}
		}

		public async Task EnableExit()
		{
			this.StepsTab.Visibility = Visibility.Visible;
			if (this._controller != null && this._controller.IsOpen)
			{
				await _controller.CloseAsync();
			}
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			this.WhenAnyValue(view => view.ViewModel.PrerequisiteFailures)
				.Subscribe(PromptPrerequisiteFailures);
		}

		public void AnimateLogo() => this.AnimateCells("Yellow", "Pink", "Blue", "Torqoise");

		private void AnimateCells(params string[] colors)
		{
			var installed = this.ViewModel.ClosingModel.Installed;
			//the logo animation is quite cheerful, let's not celebrate failure.
			if (!installed.HasValue || installed.Value == ClosingResult.Failed) return;

			var resources = Application.Current.MainWindow.Resources.MergedDictionaries;
			var cleanWindowResource = resources[0].MergedDictionaries.FirstOrDefault(m => m.Source.ToString().Contains("CleanWindow"));
			foreach (var c in colors)
			{
				var animation = cleanWindowResource[c + "Animation"] as Storyboard;
				var cell = this.Template.FindName(c + "Cell", this) as System.Windows.Shapes.Path;
				Storyboard.SetTarget(animation, cell);
				animation.Begin();
			}
		}
	}
}
