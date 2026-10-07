using System.Globalization;
using FluentValidation.Resources;
using Microsoft.Extensions.Localization;

namespace FocusTemplate.Admin.Shared.Localization;

// Looked up on every message in the UI culture of the moment, which on the server is each request's. A culture asked
// for explicitly goes to FluentValidation's own texts.
public sealed class CustomLanguageManager(IStringLocalizer<CustomLanguageManager> localizer) : LanguageManager
{
	public override string GetString(string key, CultureInfo? culture = null) =>
		(culture ?? Culture) is null && localizer[key] is { ResourceNotFound: false, } text ? text.Value : base.GetString(key, culture);
}