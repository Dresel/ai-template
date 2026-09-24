using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Auditing;

// Who is acting: the owner a station is scoped to, the author the audit columns record. The Admin API reads it from
// the request's token (HttpContextCurrentUser), FixedCurrentUser serves the tooling, the seed and the tests.
public interface ICurrentUser
{
	public UserId Id { get; }
}