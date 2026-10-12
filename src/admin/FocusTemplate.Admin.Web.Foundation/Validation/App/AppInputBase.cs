using System.Linq.Expressions;
using FocusTemplate.Admin.Web.Foundation.Validation.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace FocusTemplate.Admin.Web.Foundation.Validation.App;

public abstract class AppInputBase<TValue> : ComponentBase, IDisposable
{
	private FormMessages? registeredWith;

	private FieldRegistration? registration;

	[Parameter(CaptureUnmatchedValues = true)]
	public IReadOnlyDictionary<string, object>? Attributes { get; set; }

	[CascadingParameter]
	public IFormHost Form { get; set; } = null!;

	[Parameter]
	public string? Label { get; set; }

	// The path when the expression does not spell it: a list row's value stands for the item, Tags[1]
	[Parameter]
	public string? Path { get; set; }

	[Parameter]
	public TValue Value { get; set; } = default!;

	[Parameter]
	public EventCallback<TValue> ValueChanged { get; set; }

	// Messages find the input through the field and path it spells, without reflection. @bind-Value supplies it
	[Parameter]
	[EditorRequired]
	public Expression<Func<TValue>> ValueExpression { get; set; } = null!;

	protected string ElementId { get; } = $"input-{Guid.NewGuid():N}";

	// The input's own test id names its message line: code-error, code-warning, code-info, code-more, code-tooltip
	protected string? TestId => Attributes?.GetValueOrDefault("data-testid") as string;

	protected FieldIdentifier Field { get; private set; }

	protected IReadOnlyList<FieldMessage> Messages => Form.Messages.For(Field);

	public void Dispose()
	{
		this.registration?.Dispose();
		GC.SuppressFinalize(this);
	}

	protected abstract Task FocusAsync();

	protected virtual TValue Normalized(TValue value) => value;

	protected Task OnBlurAsync() => Form.NotifyFieldFocusLostAsync(Field);

	// A row that moved keeps its field and takes its new index. Another form or another row needs a new claim
	protected override void OnParametersSet()
	{
		Field = FieldIdentifier.Create(ValueExpression);
		string path = Path ?? FieldPath.Of(ValueExpression);

		if (this.registration is not null && ReferenceEquals(this.registeredWith, Form.Messages) &&
			this.registration.Field.Equals(Field))
		{
			this.registration.Path = path;
			return;
		}

		this.registration?.Dispose();
		this.registration = Form.Messages.Register(path, Field, FocusAsync);
		this.registeredWith = Form.Messages;
	}

	protected async Task OnValueChangedAsync(TValue value)
	{
		TValue normalized = Normalized(value);
		await ValueChanged.InvokeAsync(normalized);
		await Form.NotifyFieldChangedAsync(Field);
	}
}