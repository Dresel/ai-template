using System.Reflection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FocusTemplate.Admin.Shared.Localization;

// ResXGenerator names its classes with a suffix the resx files lack: MainLayoutLocalizations reads MainLayout.resx
public sealed class ConventionBasedStringLocalizerFactory(IOptions<LocalizationOptions> localizationOptions, ILoggerFactory loggerFactory)
	: ResourceManagerStringLocalizerFactory(localizationOptions, loggerFactory)
{
	private const string Suffix = "Localizations";

	protected override string GetResourcePrefix(TypeInfo typeInfo)
	{
		string prefix = base.GetResourcePrefix(typeInfo);
		return prefix.EndsWith(Suffix, StringComparison.Ordinal) ? prefix[..^Suffix.Length] : prefix;
	}
}