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
using Elastic.Installer.Domain.Session;
using System.Reflection;
using Elastic.Installer.UI.Progress;
using Microsoft.Deployment.WindowsInstaller;
using Elastic.Installer.Domain.Model;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Elastic.Installer.Domain.Elasticsearch.Model;
using Elastic.Installer.Domain.Elasticsearch.Model.Closing;
using Elastic.Installer.Domain.Elasticsearch.Model.Notice;
using Elastic.Installer.Domain.Elasticsearch.Model.Locations;
using Elastic.Installer.Domain.Elasticsearch.Model.Config;
using Elastic.Installer.Domain.Elasticsearch.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Elasticsearch.Configuration.FileBased;
using Elastic.Installer.Domain.Extensions;
using Elastic.Installer.UI.Elasticsearch.Steps;
using Elastic.Installer.Domain.Shared.Model.Service;
using Elastic.Installer.UI.Shared.Steps;
using Elastic.Installer.Domain.Shared.Model.Plugins;
using Elastic.Installer.Domain.Shared.Configuration.EnvironmentBased;
using Elastic.Installer.Domain.Shared.Model.Closing;
using Elastic.Installer.Domain.Elasticsearch.Model.Plugins;

namespace Elastic.Installer.UI.Elasticsearch
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow, IViewFor<ElasticsearchInstallationModel>, IEmbeddedWindow
	{
		object IViewFor.ViewModel
		{
			get { return ViewModel; }
			set { ViewModel = (ElasticsearchInstallationModel)value; }
		}

		public ElasticsearchInstallationModel ViewModel { get; set; }

		private readonly ManualResetEvent _installerStartEvent;
		private readonly ISession _session;
		private ProgressDialogController _controller;
		private InstallProgressCounter _progressCounter;
		private readonly string _currentVersion;

		public MainWindow(ElasticsearchInstallationModel setupViewModel, ManualResetEvent startEvent)
		{
			this._installerStartEvent = startEvent;
			this._session = setupViewModel.Session;
			this._currentVersion = _session.Get<string>("CurrentVersion");
			Application.Current.Resources["InstallerTitle"] = this._currentVersion;

			InitializeComponent();

			this.Title = string.Format(ViewResources.MainWindow_Title, _currentVersion);
			this.ViewModel = setupViewModel;
			this.DataContext = this.ViewModel;
			SetupViewModelCommands();
			TabNavigation();

			this.Bind(ViewModel, vm => vm.NextButtonText, view => view.NextButton.Content);

			this.WhenAny(view => view.ViewModel.CurrentStepValidationFailures, v => v.GetValue().Count)
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

		private readonly IDictionary<Type, Action<IValidatableReactiveObject, StepWithHelp>> StepModelToControl =
			new Dictionary<Type, Action<IValidatableReactiveObject, StepWithHelp>>
		{
			{ typeof(NoticeModel),
					(m, s) =>  Step(s, new NoticeView { ViewModel = m as NoticeModel }, string.Format(ViewResources.MainWindow_Help, "Elasticsearch")) },
			{ typeof(LocationsModel),
					(m, s) => Step(s, new LocationsView { ViewModel = m as LocationsModel }, ViewResources.LocationsView_Elasticsearch_Help)  },
			{ typeof(ServiceModel),
					(m, s) => Step(s, new ServiceView { ViewModel = m as ServiceModel }, ViewResources.ServiceView_Elasticsearch_Help) },
			{ typeof(ConfigurationModel),
					(m, s) => Step(s, new ConfigurationView { ViewModel = m as ConfigurationModel }, ViewResources.ConfigurationView_Elasticsearch_Help) },
			{ typeof(PluginsModel),
					(m, s) => Step(s, new PluginsView { ViewModel = m as PluginsModel }, ViewResources.PluginsView_Elasticsearch_Help) },
			{ typeof(ClosingModel),
					(m, s) => Step(s, new ClosingView { ViewModel = m as ClosingModel }, null) },
		};

		private static void Step(StepWithHelp step, UserControl view, string help)
		{
			step.Step = view;
			step.HelpText = help;
		}

		private void TabNavigation()
		{
			var defaultTabForeground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
			var hoverTabForeground = new SolidColorBrush(Color.FromRgb(0, 169, 224));
			var disabledTabForeground = new SolidColorBrush(Color.FromRgb(160, 160, 160));
			var selectedTabForeground = new SolidColorBrush(Color.FromRgb(0, 169, 224));
			var badTabForeground = new SolidColorBrush(Color.FromRgb(233, 73, 152));

			this.OneWayBind(ViewModel, vm => vm.Steps, view => view.StepsTab.ItemsSource, s => s.Select((m, i) =>
			{
				var step = new StepWithHelp
				{
					Margin = new Thickness(0, 10, 0, 0),
					VerticalAlignment = VerticalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Stretch,
				};
				StepModelToControl[m.GetType()](m, step);
				var headerLabel = new Label
				{
					Content = m.Header,
					FontSize = 26,
					Margin = new Thickness(-4, 0, 0, 0)
				};

				Action reset = () =>
				{
					headerLabel.Cursor = Cursors.Arrow;
					headerLabel.Foreground = defaultTabForeground;
					if (this.ViewModel.TabSelectedIndex == i)
						headerLabel.Foreground = selectedTabForeground;
					if (!this.ViewModel.Steps[i].IsValid)
						headerLabel.Foreground = badTabForeground;
				};

				headerLabel.MouseLeave += (ss, e) => reset();
				headerLabel.MouseLeftButtonDown += (ss, e) => reset();
				headerLabel.MouseEnter += (ss, e) =>
				{
					if (this.ViewModel.TabSelectedIndex == i) return;
					headerLabel.Cursor = Cursors.Hand;
					headerLabel.Foreground = hoverTabForeground;
				};

				var tab = new MetroTabItem { Content = step, Header = headerLabel };
				return tab;
			}), null);

			this.Bind(ViewModel, vm => vm.TabSelectedIndex, view => view.StepsTab.SelectedIndex);

			//Make sure tabs are colored according to state
			//selected is blue, errror is red, smaller then max allowed tab is disabled and grey
			this.WhenAnyValue(vm => vm.ViewModel.TabSelectedIndex, vm => vm.ViewModel.TabSelectionMax)
				.Subscribe(t =>
				{
					var selected = t.Item1;
					var max = t.Item2;
					var i = 0;
					foreach (var child in this.StepsTab.Items.OfType<MetroTabItem>())
					{
						var label = child.FindChildren<Label>().First();
						child.IsEnabled = true;
						label.IsEnabled = true;

						label.Foreground = defaultTabForeground;
						if (i == selected)
							label.Foreground = selectedTabForeground;
						if (!this.ViewModel.Steps[i].IsValid)
							label.Foreground = badTabForeground;
						if (i > max)
						{
							child.IsEnabled = false;
							label.IsEnabled = false;
							label.Foreground = disabledTabForeground;
						}
						if (selected >= this.ViewModel.Steps.Count - 1)
							child.Visibility = Visibility.Collapsed;
						i++;
					}
				});
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
				stepWithHelp.ToggleHelp();
			});

			this.ViewModel.ShowLicenseBlurb.Subscribe(async x =>
			{
				await this.ShowMessageAsync(
					ViewResources.MainWindow_LicenseHeader,
					ViewResources.MainWindow_LicenseInformation,
					MessageDialogStyle.Affirmative,
					new MetroDialogSettings()).ConfigureAwait(true);
			});

			this.ViewModel.ShowCurrentStepErrors.Subscribe(async x =>
			{
				var message = string.Join("\r\n", this.ViewModel.CurrentStepValidationFailures.Select(v => v.ErrorMessage.ValidationMessage()));
				await this.ShowMessageAsync(
					ViewResources.MainWindow_ValidationErrors,
					message,
					MessageDialogStyle.Affirmative,
					new MetroDialogSettings()).ConfigureAwait(true);
			});
			this.ViewModel.Exit.Subscribe(x =>
			{
				if (this.ViewModel.ClosingModel.OpenDocumentationAfterInstallation)
					Process.Start("https://www.elastic.co/guide/index.html");
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
		}

		private void JumpToInstallationResult()
		{
			//disable all tabs, whether the installer succeeds or fails the only allowed action afterwards is exiting.
			this.ViewModel.TabSelectedIndex = this.ViewModel.Steps.Count - 1;
		}

		private void RecheckPrequisites()
		{
			var javaState = new JavaEnvironmentStateProvider();
			var esState = new ElasticsearchEnvironmentStateProvider();
			var javaConfig = new JavaConfiguration(javaState);
			var esConfig = ElasticsearchYamlConfiguration.FromFolder(esState.ConfigDirectory);

			this.ViewModel.BadElasticsearchYamlFile = esConfig.FoundButNotValid;
			this.ViewModel.JavaInstalled = javaConfig.JavaInstalled;
			this.ViewModel.JavaMisconfigured = javaConfig.JavaMisconfigured;
		}

		private void PromptPrerequisiteFailures(IList<ValidationFailure> failures)
		{
			if (failures.Count == 0) return;

			this.JumpToInstallationResult();
			this.ViewModel.ClosingModel.Installed = ClosingResult.Preempted;
			this.ViewModel.ClosingModel.PrerequisiteFailures = this.ViewModel.PrerequisiteFailures;
			this.ViewModel.ClosingModel.PrerequisiteFailureMessages = this.ViewModel.PrerequisiteFailures.Select(v => v.ErrorMessage);
		}

		private async Task<IObservable<ClosingResult>> InstallAsync()
		{
			SetSessionValues(this.ViewModel);
			SetSessionValues(this.ViewModel.NoticeModel);
			SetSessionValues(this.ViewModel.LocationsModel);
			SetSessionValues(this.ViewModel.ConfigurationModel);
			SetSessionValues(this.ViewModel.ServiceModel);
			SetSessionValues(this.ViewModel.PluginsModel);

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
					var value = this.ViewModel.ParsedArguments.MsiString((pi.GetValue(viewModel, null)));
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
				.Subscribe(failures => PromptPrerequisiteFailures(failures));
		}

		public void AnimateLogo() => this.AnimateCells("Yellow", "Pink", "Blue", "Torqoise");

		public void AnimateCells(params string[] colors)
		{
			var installed = this.ViewModel.ClosingModel.Installed;
			//the logi animation is quite cheerful, let's not celebrate failure.
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
