using SpatialFocus.Data.Foundation.Auditing;
using SpatialFocus.Primitives;

namespace SpatialFocus.Data.Entities;

public sealed class GroupMember : IAuditable
{
	public required GroupId GroupId { get; init; }

	public required UserId UserId { get; init; }
}