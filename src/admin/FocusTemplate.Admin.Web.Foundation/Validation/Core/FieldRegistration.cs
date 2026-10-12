using Microsoft.AspNetCore.Components.Forms;

namespace FocusTemplate.Admin.Web.Foundation.Validation.Core;

// A field's claim on the messages at its path, for as long as the input or AppFieldMessages showing them is rendered
public sealed class FieldRegistration : IDisposable
{
	private readonly FormMessages owner;

	internal FieldRegistration(FormMessages owner, string path, FieldIdentifier field, Func<Task>? focus)
	{
		this.owner = owner;
		Path = path;
		Field = field;
		Focus = focus;
	}

	public FieldIdentifier Field { get; }

	// Settable because a list row's input moves to the index its row has after a row above it is removed
	public string Path { get; set; }

	internal Func<Task>? Focus { get; }

	public void Dispose() => this.owner.Unregister(this);
}