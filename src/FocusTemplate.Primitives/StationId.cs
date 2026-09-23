namespace FocusTemplate.Primitives;

public readonly partial struct StationId
{
	public static StationId New() => From(Guid.CreateVersion7());
}