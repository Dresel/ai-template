namespace SpatialFocus.Data.Foundation;

// The schema a context's tables live in, read from its type: EF builds the model once per type, and a pooled context
// takes nothing but its options. The model's default schema and the registration's history table both read it.
public interface IHasSchema
{
	public static abstract string Schema { get; }
}