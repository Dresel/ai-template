using System.ComponentModel.DataAnnotations;

namespace FocusTemplate.Admin.Web.Bff;

public sealed class OidcOptions
{
	public const string SectionName = "Oidc";

	[Required]
	public string Authority { get; init; } = string.Empty;

	[Required]
	public string ClientId { get; init; } = string.Empty;

	[Required]
	public string ClientSecret { get; init; } = string.Empty;
}