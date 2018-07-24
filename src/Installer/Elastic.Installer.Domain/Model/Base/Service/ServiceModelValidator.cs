using System;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using Elastic.Installer.Domain.Properties;
using FluentValidation;
using Microsoft.Win32;

namespace Elastic.Installer.Domain.Model.Base.Service
{
	public class ServiceModelValidator : AbstractValidator<ServiceModel>
	{
		private const string RegisteredOwner = "RegisteredOwner";
		private const string RegisteredOrganization = "RegisteredOrganization";
		public static readonly string UserNotEmptyWhenUseExistingUser = TextResources.ServiceModelValidator_User_NotEmpty_WhenUseExistingUser;
		public static readonly string PasswordNotEmptyWhenUseExistingUser = TextResources.ServiceModelValidator_Password_NotEmpty_WhenUseExistingUser;
		public static readonly string MultipleRunAsTypes = TextResources.ServiceModelValidator_MultipleRunAsTypes;
		public static readonly string UserOrPasswordIncorrect = TextResources.ServiceModelValidator_User_Or_Password_Incorrect;
		public static readonly string CredentialsFailed = TextResources.ServiceModelValidator_CredentialsFailed;

		public ServiceModelValidator()
		{
			RuleFor(vm => vm.User)
				.NotEmpty()
				.When(vm => vm.UseExistingUser).WithMessage(UserNotEmptyWhenUseExistingUser);
			RuleFor(vm => vm.Password)
				.NotEmpty()
				.When(vm => vm.UseExistingUser).WithMessage(PasswordNotEmptyWhenUseExistingUser);

			RuleFor(vm => vm.ManualValidationPassed)
				.Must((vm, s) =>vm.ManualValidationPassed)
				.When(vm => vm.UseExistingUser).WithMessage(CredentialsFailed);

			RuleFor(vm => vm.UseLocalSystem)
				.Must((vm, l) => new[] { vm.UseLocalSystem, vm.UseExistingUser, vm.UseNetworkService }.Count(u => u) == 1).WithMessage(MultipleRunAsTypes);
		}

		public bool ValidateCredentials(string user, string password)
		{
			if (string.IsNullOrWhiteSpace(user)) return false;

			var userParts = user.Split(new [] { '\\' }, 2);
			var domain = string.Empty;
			if (userParts.Length > 1)
			{
				domain = userParts[0];
				user = userParts[1];
			}

			try
			{
				if (!string.IsNullOrEmpty(domain) && domain != "." && domain != Environment.UserDomainName)
				{
					using (var context = new PrincipalContext(ContextType.Domain, domain))
						return context.ValidateCredentials(user, password);
				}

				using (var context = new PrincipalContext(ContextType.Machine))
					return context.ValidateCredentials(user, password);
			}
			catch (FileNotFoundException e)
			{
				if (e.Source == "Active Directory")
				{
					// Being bitten by the this Windows OS bug: https://developer.microsoft.com/en-us/microsoft-edge/platform/issues/6638841/
					// Add registry keys to HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion
					// if missing
					var currentVersionKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false);
					var registeredOwner = currentVersionKey.GetValue(RegisteredOwner);
					var registeredOrganisation = currentVersionKey.GetValue(RegisteredOrganization);

					var currentVersion64BitKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion", true);
					var registeredOwner64Bit = currentVersion64BitKey.GetValue(RegisteredOwner);
					var registeredOrganisation64Bit = currentVersion64BitKey.GetValue(RegisteredOrganization);

					if (registeredOwner64Bit == null)
					{
						currentVersion64BitKey.SetValue(RegisteredOwner, registeredOwner ?? string.Empty);
					}

					if (registeredOrganisation64Bit == null)
					{
						currentVersion64BitKey.SetValue(RegisteredOrganization, registeredOrganisation ?? string.Empty);
					}
				}

				return false;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}