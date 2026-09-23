using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Auditing;

// Who is acting: the owner a station is scoped to, the author the audit columns record. Until authentication lands
// the only implementation is FixedCurrentUser; the interface stays.
public interface ICurrentUser
{
	public UserId Id { get; }
}