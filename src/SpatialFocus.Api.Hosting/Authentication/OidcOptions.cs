using System.ComponentModel.DataAnnotations;

namespace SpatialFocus.Api.Hosting.Authentication;

public sealed class OidcOptions
{
	public const string SectionName = "Oidc";

	[Required]
	public string Authority { get; init; } = string.Empty;

	[Required]
	public string Audience { get; init; } = string.Empty;
}