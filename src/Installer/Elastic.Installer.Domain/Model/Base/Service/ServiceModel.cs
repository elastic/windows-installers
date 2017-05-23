using System;
using System.Text;
using System.Threading.Tasks;
using Elastic.Installer.Domain.Configuration.Service;
using Elastic.Installer.Domain.Configuration.Wix;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Base.Service
{
	public class ServiceModel : StepBase<ServiceModel, ServiceModelValidator>
	{
		public static readonly bool DefaultServiceStart = true;
		public static readonly bool DefaultServiceRunAsNetworkService = false;
		public static readonly bool DefaultServiceInstall = true;
		public static readonly bool DefaultServiceAutomatic = true;
		public static readonly bool DefaultUseLocalSystem = true;
		public static readonly bool DefaultUseExistingUser = false;
		public static readonly bool DefaultUseNetworkService = false;
		public static readonly string DefaultUser = null;
		public static readonly string DefaultPassword = null;

		private readonly bool _alreadyInstalled;
		private readonly bool _sawService;

		public ServiceModel(IServiceStateProvider serviceStateProvider, VersionConfiguration versionConfig)
		{
			this._alreadyInstalled = versionConfig.AlreadyInstalled;
			this._sawService = serviceStateProvider.SeesService;
			this.IsRelevant = !this._alreadyInstalled || (this._alreadyInstalled && !this._sawService);
			this.Header = "Service";
			this.Refresh();
			this.WhenAny(vm => vm.User, vm => vm.Password,
				(u, p) => string.IsNullOrEmpty(u.Value) && string.IsNullOrEmpty(p.Value))
				.Subscribe(b =>
				{
					if (b) return;
					this.UseExistingUser = true;
					this.UseLocalSystem = false;
					this.UseNetworkService = false;
				});

			this.WhenAny(vm => vm.InstallAsService, i => i.Value)
				.Subscribe(b =>
				{
					if (!b)
					{
						_internalRefresh = true;
						this.Refresh();
					}
				});

			this.ValidateCredentials = ReactiveCommand.CreateAsyncTask(async _ => {
				this.ValidatingCredentials = true;
				var valid = await Task.Run(() => this.Validator.ValidateCredentials(this.User, this.Password));
				this.ManualValidationPassed = valid;
				this.ValidatingCredentials = false;
			});
		}

		public ReactiveCommand<System.Reactive.Unit> ValidateCredentials { get; }

		private bool _internalRefresh;
		public sealed override void Refresh()
		{
			this.User = DefaultUser;
			this.Password = DefaultPassword;
			this.ManualValidationPassed = true;
			this.StartAfterInstall = DefaultServiceStart;
			this.StartWhenWindowsStarts = DefaultServiceAutomatic;
			this.UseLocalSystem = DefaultUseLocalSystem;
			this.UseExistingUser = DefaultUseExistingUser;
			this.UseNetworkService = DefaultUseNetworkService;
			if (!this._internalRefresh) 
			{
				this.InstallAsService = !this._alreadyInstalled || this._sawService;
			}
			this._internalRefresh = false;
		}

		// ReactiveUI conventions do not change
		// ReSharper disable InconsistentNaming
		// ReSharper disable ArrangeTypeMemberModifiers
		bool installAsService = true;
		[StaticArgument(nameof(InstallAsService))]
		public bool InstallAsService
		{
			get => installAsService;
			set => this.RaiseAndSetIfChanged(ref installAsService, value);
		}

		bool startAfterInstall = true;
		[StaticArgument(nameof(StartAfterInstall))]
		public bool StartAfterInstall
		{
			get => startAfterInstall;
			set => this.RaiseAndSetIfChanged(ref startAfterInstall, value);
		}

		bool startWhenWindowsStarts = true;
		[StaticArgument(nameof(StartWhenWindowsStarts))]
		public bool StartWhenWindowsStarts
		{
			get => startWhenWindowsStarts;
			set => this.RaiseAndSetIfChanged(ref startWhenWindowsStarts, value);
		}

		bool useLocalSystem = true;
		[StaticArgument(nameof(UseLocalSystem))]
		public bool UseLocalSystem
		{
			get => useLocalSystem;
			set => this.RaiseAndSetIfChanged(ref useLocalSystem, value);
		}

		bool useExistingUser;
		[Argument(nameof(UseExistingUser))]
		public bool UseExistingUser
		{
			get => useExistingUser;
			set => this.RaiseAndSetIfChanged(ref useExistingUser, value);
		}

		bool useNetworkService;
		[StaticArgument(nameof(UseNetworkService))]
		public bool UseNetworkService
		{
			get => useNetworkService;
			set => this.RaiseAndSetIfChanged(ref useNetworkService, value);
		}

		string user;
		[Argument(nameof(User))]
		public string User
		{
			get => user;
			set => this.RaiseAndSetIfChanged(ref user, value);
		}

		string password;
		[Argument(nameof(Password))]
		public string Password
		{
			get => password;
			set => this.RaiseAndSetIfChanged(ref password, value);
		}
		bool manualValidationPassed;
		public bool ManualValidationPassed
		{
			get => manualValidationPassed;
			set => this.RaiseAndSetIfChanged(ref manualValidationPassed, value);
		}

		bool validatingCredentials;
		public bool ValidatingCredentials
		{
			get => validatingCredentials;
			set => this.RaiseAndSetIfChanged(ref validatingCredentials, value);
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.AppendLine(nameof(ServiceModel));
			sb.AppendLine($"- {nameof(IsValid)} = " + IsValid);
			sb.AppendLine($"- {nameof(InstallAsService)} = " + InstallAsService);
			sb.AppendLine($"- {nameof(StartAfterInstall)} = " + StartAfterInstall);
			sb.AppendLine($"- {nameof(StartWhenWindowsStarts)} = " + StartWhenWindowsStarts);
			sb.AppendLine($"- {nameof(UseLocalSystem)} = " + UseLocalSystem);
			sb.AppendLine($"- {nameof(UseExistingUser)} = " + UseExistingUser);
			sb.AppendLine($"- {nameof(UseNetworkService)} = " + UseNetworkService);
			sb.AppendLine($"- {nameof(User)} = " + User);
			sb.AppendLine($"- {nameof(Password)} = " + !string.IsNullOrWhiteSpace(Password));
			return sb.ToString();
		}
	}
}