namespace SpatialFocus.Primitives;

public readonly partial struct AlertId
{
	public static AlertId New() => From(Guid.CreateVersion7());
}