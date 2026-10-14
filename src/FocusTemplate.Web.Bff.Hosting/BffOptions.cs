using System.ComponentModel.DataAnnotations;

namespace FocusTemplate.Web.Bff.Hosting;

// What differs between the BFFs of two products
public sealed class BffOptions
{
	public const string SectionName = "Bff";

	// Cookies on localhost do not depend on the port, so two products side by side need a name each. The __Host- prefix
	// makes browsers require Secure, Path=/ and no Domain, so no subdomain can overwrite the session
	[Required]
	[RegularExpression("^__Host-.+")]
	public string SessionCookie { get; init; } = string.Empty;

	// The API that answers /users/me, by its service discovery name (https+http://admin-api/)
	[Required]
	public string Api { get; init; } = string.Empty;
}