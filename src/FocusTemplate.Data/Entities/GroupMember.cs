using FocusTemplate.Data.Auditing;
using FocusTemplate.Primitives;

namespace FocusTemplate.Data.Entities;

public sealed class GroupMember : IAuditable
{
	public required GroupId GroupId { get; init; }

	public required UserId UserId { get; init; }
}