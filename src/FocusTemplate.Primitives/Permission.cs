namespace FocusTemplate.Primitives;

public readonly partial struct Permission
{
	// The BFF hands the API's permissions to the client as claims of this type
	public const string ClaimType = "permission";
}