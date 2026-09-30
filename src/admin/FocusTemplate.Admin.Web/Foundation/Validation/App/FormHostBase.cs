using FluentValidation;
using FocusTemplate.Admin.Shared;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FocusTemplate.Admin.Web.Foundation.Validation.App;

public abstract class FormHostBase<TModel> : ComponentBase, IFormHost, IDisposable
	where TModel : class
{
	private TModel? model;

	private FormValidation validation = null!;

	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	public FormMessages Messages { get; private set; } = null!;

	[Parameter]
	[EditorRequired]
	public TModel Model { get; set; } = null!;

	[Parameter]
	public EventCallback OnValidSubmit { get; set; }

	// For members the view model names differently than the contract: the wire path to the view model's own
	[Parameter]
	public IReadOnlyDictionary<string, string>? Renames { get; set; }

	[Parameter]
	[EditorRequired]
	public IValidator<TModel> Validator { get; set; } = null!;

	public Task ClearServerErrorsAsync()
	{
		Messages.ClearServerErrors();
		return RefreshAsync();
	}

	public void Dispose()
	{
		this.validation?.Dispose();
		GC.SuppressFinalize(this);
	}

	public async Task NotifyFieldChangedAsync(FieldIdentifier field)
	{
		Task<bool> validated = this.validation.FieldChangedAsync(field);
		await RefreshAsync();

		if (await validated)
		{
			await RefreshAsync();
		}
	}

	public async Task NotifyFieldFocusLostAsync(FieldIdentifier field)
	{
		if (await this.validation.FieldFocusLostAsync(field))
		{
			await RefreshAsync();
		}
	}

	public virtual Task RefreshAsync()
	{
		StateHasChanged();
		return Task.CompletedTask;
	}

	public async Task ShowServerErrorsAsync(ValidationProblemDetails problem)
	{
		Messages.SetServerErrors(problem);

		await RefreshAsync();
		await Messages.FocusFirstInvalidAsync();
	}

	// Another model is another form: nothing touched, no messages, no run still waiting; the inputs claim their paths in
	// the new messages
	protected override void OnParametersSet()
	{
		if (!ReferenceEquals(this.model, Model))
		{
			this.model = Model;
			this.validation?.Dispose();
			Messages = new FormMessages(Renames);
			this.validation = new FormValidation(
				Messages,
				(paths, cancellationToken) => paths is null
					? Validator.ValidateAsync(Model, cancellationToken)
					: Validator.ValidateAsync(
						Model,
						options => options.IncludeProperties([.. paths,]),
						cancellationToken),
				(Validator as ValidatorBase<TModel>)?.Dependencies);
		}
	}

	protected async Task SubmitAsync()
	{
		bool? valid = await this.validation.ValidateAllAsync();
		await RefreshAsync();

		if (valid is true)
		{
			await OnValidSubmit.InvokeAsync();
		}
		else if (valid is false)
		{
			await Messages.FocusFirstInvalidAsync();
		}
	}
}