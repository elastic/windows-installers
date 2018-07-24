using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Elastic.Installer.Domain.Model.Base;
using Elastic.Installer.Domain.Model.Elasticsearch.XPack;
using FluentValidation.Results;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model
{
	public class ModelArgumentParser
	{
		public List<ValidationFailure> ValidationFailures { get; } = new List<ValidationFailure>();
		public List<ModelArgument> ViewModelArguments { get; } = new List<ModelArgument>();
		public List<ModelArgument> AllConfigurableArguments { get; }

		private static readonly char[] VariableSplitCharacters = { '=' };

		public ModelArgumentParser(IList<IValidatableReactiveObject> viewModels, string[] args)
		{
			this.AllConfigurableArguments = KnownModelArguments(viewModels);
			if (args == null || args.Length == 0)
				return;

			var userArguments = (
				from a in args
				where !a.StartsWith("/", StringComparison.OrdinalIgnoreCase)
				let parts = a.Split(VariableSplitCharacters, 2)
				where parts.Length == 2
				select new ModelArgument { Key = parts[0], Value = Unquote(parts[1]) }).ToArray();

			if (userArguments.Length == 0)
				return;

			if (this.AllConfigurableArguments == null || this.AllConfigurableArguments.Count == 0)
				return;

			var seenUserArguments = new HashSet<string>();
			foreach (var userArg in userArguments)
			{
				if (!this.AllConfigurableArguments.Any(a => a.PropertyInfo.Name.Equals(userArg.Key, StringComparison.OrdinalIgnoreCase)))
				{
					var error = $"{userArg.Key} specified but not a known variable";
					this.ValidationFailures.Add(new ValidationFailure("VariableArguments", error));
					continue;
				}

				if (seenUserArguments.Contains(userArg.Key, StringComparer.OrdinalIgnoreCase))
				{
					var error = $"{userArg.Key} specified multiple times which causes ambiguity in which value should be taken";
					this.ValidationFailures.Add(new ValidationFailure("VariableArguments", error));
					continue;
				}

				seenUserArguments.Add(userArg.Key);
				var viewModelArg = this.AllConfigurableArguments.First(a => a.PropertyInfo.Name.Equals(userArg.Key, StringComparison.OrdinalIgnoreCase));
				viewModelArg.Key = userArg.Key;
				viewModelArg.Value = userArg.Value;
				viewModelArg.Attribute = viewModelArg.PropertyInfo.GetCustomAttribute<ArgumentAttribute>();
				this.ViewModelArguments.Add(viewModelArg);
			}

			this.ApplyViewModelArguments();
		}

		public string Unquote(string str)
		{
			if (string.IsNullOrEmpty(str) || str[0] != '"') return str;
			return str.Trim('"');
		}

		public string MsiString(object v)
		{
			switch (v)
			{
				case null:
					return null;
				case string s:
					return s;
				// handles both int and an int? that has a value
				case int i:
					return i.ToString(CultureInfo.InvariantCulture);
				case ulong u:
					return u.ToString(CultureInfo.InvariantCulture);
				case IEnumerable<string> values:
					return values.Any(s => !string.IsNullOrEmpty(s)) 
						? string.Join(",", values.Where(s => !string.IsNullOrEmpty(s)))
						: null;
				case bool b:
					return b.ToString().ToLowerInvariant();
				case Enum _:
					return Enum.GetName(v.GetType(), v);
			}
		
			throw new Exception($"{v.GetType().FullName} has no supported getter");
		}

		public IEnumerable<ModelArgument> ToMsiParams()
		{
			var msiParams = new List<ModelArgument>();
			foreach (var a in this.AllConfigurableArguments)
			{
				var getter = GetterDelegate(a);
				var p = a.PropertyType;
				a.Key = a.Attribute.Name;

				if (p == typeof(string))
					a.Value = MsiString(((Func<string>)getter)());
				else if (p == typeof(int))
					a.Value = MsiString(((Func<int>)getter)());
				else if (p == typeof(int?))
					a.Value = MsiString(((Func<int?>)getter)());
				else if (p == typeof(ulong))
					a.Value = MsiString(((Func<ulong>)getter)());
				else if (p == typeof(IEnumerable<string>))
					a.Value = MsiString(((Func<IEnumerable<string>>)getter)());
				else if (p == typeof(ReactiveList<string>))
					a.Value = MsiString(((Func<ReactiveList<string>>)getter)());
				else if (p == typeof(bool))
					a.Value = MsiString(((Func<bool>)getter)());
				else if (p == typeof(XPackLicenseMode))
					a.Value = MsiString(((Func<XPackLicenseMode>)getter)());
				else
					throw new Exception($"{p.FullName} has no supported getter");

				msiParams.Add(a);
			}

			return msiParams;
		}

		public string ToMsiParamsString()
		{
			var parameters = this.ToMsiParams();
			return string.Join(" ", parameters.Select(kv => $"{kv.Key}=\"{kv.Value?.Replace("\"", "\\\"")}\""));
		}

		protected static IDictionary<Type, IEnumerable<string>> GetArgumentsByModel(Type[] expectedTypes)
		{
			var argumentsByType = new Dictionary<Type, IEnumerable<string>>();
			foreach (var type in expectedTypes)
			{
				var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				var viewModelProperties = GetProperties(type);
				foreach (var p in viewModelProperties)
				{
					var name = p.GetCustomAttribute<ArgumentAttribute>().Name;
					if (seenNames.Contains(name))
						throw new ArgumentException($"Argument {name} for property {p.Name} can not be reused as argument option on {type.Name}");
					seenNames.Add(p.Name.ToUpperInvariant());
				}
				argumentsByType.Add(type, seenNames);
			}
			return argumentsByType;
		}

		protected static IEnumerable<string> GetAllArguments(Type[] expectedTypes)
		{
			var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var type in expectedTypes)
			{
				var viewModelProperties = GetProperties(type);
				foreach (var p in viewModelProperties)
				{
					var name = p.GetCustomAttribute<ArgumentAttribute>().Name;
					if (!seenNames.Add(name))
					{
						throw new ArgumentException($"Argument {name} for property {p.Name} can not be reused as argument option on {type.Name}");
					}
				}
			}

			return seenNames;
		}
		private void ApplyViewModelArguments()
		{
			foreach (var a in this.ViewModelArguments)
			{
				var setter = SetterDelegate(a);
				var p = a.PropertyType;

				//do not unset dynamic values
				if (a.Attribute.IsDynamic && string.IsNullOrWhiteSpace(a.Value)) continue;

				if (p == typeof(string))
				{
					((Action<string>)setter)(a.Value);
				}
				else if (p == typeof(int))
				{
					if (int.TryParse(a.Value.ToLowerInvariant(), out var i))
						((Action<int>)setter)(i);
					else
						this.ValidationFailures.Add(new ValidationFailure("VariableArguments", $"can not convert {a.Value} to an int to apply to {a.Key}"));
				}
				else if (p == typeof(int?))
				{
					if (string.IsNullOrWhiteSpace(a.Value)) ((Action<int?>)setter)(null);
					else if (int.TryParse(a.Value.ToLowerInvariant(), out var i))
						((Action<int?>)setter)(i);
					else
						this.ValidationFailures.Add(new ValidationFailure("VariableArguments", $"can not convert {a.Value} to a nullable int to apply to {a.Key}"));
				}
				else if (p == typeof(ulong))
				{
					if (ulong.TryParse(a.Value.ToLowerInvariant(), out var i))
						((Action<ulong>)setter)(i);
					else
						this.ValidationFailures.Add(new ValidationFailure("VariableArguments", $"can not convert {a.Value} to an ulong to apply to {a.Key}"));
				}
				else if (p == typeof(IEnumerable<string>))
				{
					var value = a.Value.Split(',').Select(v => v.Trim()).ToList();
					((Action<IEnumerable<string>>)setter)(value);
				}
				else if (p == typeof(ReactiveList<string>))
				{
					((Action<ReactiveList<string>>)setter)(new ReactiveList<string>(a.Value.Split(',').Select(v => v.Trim())));
				}
				else if (p == typeof(XPackLicenseMode))
				{
					if (string.IsNullOrWhiteSpace(a.Value)) 
						((Action<XPackLicenseMode>)setter)(XPackModel.DefaultXPackLicenseMode);
					else if (Enum.TryParse<XPackLicenseMode>(a.Value, ignoreCase: true, result: out var licenseMode))
						((Action<XPackLicenseMode>)setter)(licenseMode);
				}
				else if (p == typeof(bool))
				{
					var s = ((Action<bool>)setter);
					if (bool.TryParse(a.Value.ToLowerInvariant(), out var b))
						s(b);
					else if (a.Value == "1")
						s(true);
					else if (a.Value == "0")
						s(false);
					else
						this.ValidationFailures.Add(new ValidationFailure("VariableArguments", $"can not convert {a.Value} to a bool to apply to {a.Key}"));
				}
				else
					throw new Exception($"{p.FullName} not a supported setter");
			}
		}

		private static Delegate SetterDelegate(ModelArgument a)
		{
			var delegateType = typeof(Action<>).MakeGenericType(a.PropertyType);
			var setMethod = a.PropertyInfo.GetSetMethod(true);
			var setter = setMethod.CreateDelegate(delegateType, a.Model);
			return setter;
		}

		private static Delegate GetterDelegate(ModelArgument a)
		{
			var delegateType = typeof(Func<>).MakeGenericType(a.PropertyType);
			var getMethod = a.PropertyInfo.GetGetMethod(true);
			var getter = getMethod.CreateDelegate(delegateType, a.Model);
			return getter;
		}

		private static List<ModelArgument> KnownModelArguments(IList<IValidatableReactiveObject> models)
		{
			var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var viewModelArguments = new List<ModelArgument>();
			foreach (var model in models)
			{
				var viewModelProperties = GetProperties(model.GetType());
				foreach (var p in viewModelProperties)
				{
					if (!seenNames.Add(p.Name))
						throw new ArgumentException(
							$"{p.Name} can not be reused as argument option on {model.GetType().Name} as it already exists as a property on another model");
					viewModelArguments.Add(new ModelArgument
					{
						Attribute = p.GetCustomAttribute<ArgumentAttribute>(),
						PropertyInfo = p,
						PropertyType = p.PropertyType,
						Model = model
					});
				}
			}

			return viewModelArguments;
		}

		public static IEnumerable<PropertyInfo> GetProperties(Type type) =>
			from p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			let attribute = p.GetCustomAttribute<ArgumentAttribute>()
			where attribute != null
			select p;
	}
}