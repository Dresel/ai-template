namespace FocusTemplate.Data;

public static class SearchPattern
{
	// The patterns are escaped with it, so it is the escape argument of every EF.Functions.ILike that takes one
	public const string EscapeCharacter = "\\";

	public static string? ContainingOrDefault(string? text) =>
		string.IsNullOrEmpty(text) ? null : $"%{Escape(text)}%";

	// % and _ are wildcards in LIKE, yet a search for "50%" means the percent sign
	private static string Escape(string text) =>
		text.Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
			.Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
			.Replace("_", EscapeCharacter + "_", StringComparison.Ordinal);
}