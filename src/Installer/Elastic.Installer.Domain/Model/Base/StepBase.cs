using FluentValidation;
using ReactiveUI;

namespace Elastic.Installer.Domain.Model.Base
{
	public interface IStep : IValidatableReactiveObject
	{
		/// <summary>Whether this model is relevant in the global scheme of things</summary>
		bool IsRelevant { get; }

		bool IsVisible { get; }
		
		bool IsSelected { get; set;  }

		string Header { get; }
	}

	public abstract class StepBase<TModel, TModelValidator> : ValidatableReactiveObjectBase<TModel, TModelValidator>, IStep
		where TModel : ValidatableReactiveObjectBase<TModel, TModelValidator>
		where TModelValidator : AbstractValidator<TModel>, new()
	{
		bool isRelevant = true;
		public bool IsRelevant
		{
			get => isRelevant;
			protected set => this.RaiseAndSetIfChanged(ref isRelevant, value);
		}
		bool isVisible = true;
		public bool IsVisible
		{
			get => isVisible;
			protected set => this.RaiseAndSetIfChanged(ref isVisible, value);
		}
		bool isSelected = true;
		public bool IsSelected
		{
			get => isSelected;
			set => this.RaiseAndSetIfChanged(ref isSelected, value);
		}
		string header = string.Empty;
		public string Header
		{
			get => header;
			protected set => this.RaiseAndSetIfChanged(ref header, value);
		}
	}
}