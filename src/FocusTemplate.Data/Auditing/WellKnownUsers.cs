using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Auditing;

// Users that exist by convention rather than in a directory. All sit at timestamp zero of the UUID v7 layout, before
// any id minted at run time, and differ only in the tail; none is Guid.Empty, which is the Unspecified sentinel.
public static class WellKnownUsers
{
	public static UserId System { get; } = UserId.From(new Guid("00000000-0000-7000-8000-000000000000"));

	// The developer login of the imported Keycloak realm
	public static UserId Developer { get; } = UserId.From(new Guid("00000000-0000-7000-8000-000000000001"));
}