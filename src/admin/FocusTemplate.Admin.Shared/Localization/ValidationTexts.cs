using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FocusTemplate.Admin.Shared.Localization;

// The generated rules and FluentValidation keep their texts in process-wide settings, which outlive a host. So they get a
// localizer factory of their own, which no host disposes with its container.
public static class ValidationTexts
{
	private static readonly ConventionBasedStringLocalizerFactory Localizers = new(
		Options.Create(new LocalizationOptions()),
		NullLoggerFactory.Instance);

	// Both hosts call it at startup, since the forms run the same rules in the browser
	public static void UseResources()
	{
		AdminTexts.DisplayName = Lookup;
		AdminTexts.Message = (type, member, code) => Lookup(type, $"{member}.{code}");
		ValidatorOptions.Global.LanguageManager = new CustomLanguageManager(new StringLocalizer<CustomLanguageManager>(Localizers));
	}

	// The resx named after the contract type, DemoProfileRequest.de.resx, keys Code and Code.pattern
	private static string? Lookup(Type type, string key) =>
		Localizers.Create(type)[key] is { ResourceNotFound: false, } text ? text.Value : null;
}