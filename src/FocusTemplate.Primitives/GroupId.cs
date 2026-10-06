namespace FocusTemplate.Primitives;

public readonly partial struct GroupId
{
	public static GroupId New() => From(Guid.CreateVersion7());
}