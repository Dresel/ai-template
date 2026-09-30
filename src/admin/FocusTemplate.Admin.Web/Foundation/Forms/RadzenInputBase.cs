using FluentValidation;
using FocusTemplate.Admin.Web.Foundation.Validation.App;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace FocusTemplate.Admin.Web.Foundation.Forms;

public abstract class RadzenInputBase<TValue> : AppInputBase<TValue>
{
	protected string MessageId { get; } = $"message-{Guid.NewGuid():N}";

	protected IReadOnlyDictionary<string, object> InputAttributes
	{
		get
		{
			Dictionary<string, object> attributes = new(Attributes ?? new Dictionary<string, object>())
			{
				["onblur"] = EventCallback.Factory.Create<FocusEventArgs>(this, OnBlurAsync),
			};

			if (Messages.Count > 0)
			{
				Severity highestSeverity = Messages.Min(message => message.Severity);
				attributes["aria-describedby"] = MessageId;

				if (highestSeverity == Severity.Error)
				{
					attributes["aria-invalid"] = "true";
				}

				if (highestSeverity != Severity.Info)
				{
					attributes["data-severity"] = Severities.Name(highestSeverity);
				}
			}

			return attributes;
		}
	}
}