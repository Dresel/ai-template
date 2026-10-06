using FocusTemplate.Primitives;

namespace FocusTemplate.Admin.Web.Features.UserManagement;

// Until the spec's descriptions reach the client, a permission reads off its name: the slice, then the action
internal sealed record PermissionLabel(string Section, string Action)
{
	public static PermissionLabel Of(Permission permission)
	{
		string name = permission.Value;
		int dot = name.IndexOf('.', StringComparison.Ordinal);

		return new PermissionLabel(Words(name[..dot]), Words(name[(dot + 1)..]));
	}

	private static bool IsAcronym(string word) => word.Length > 1 && word.All(char.IsUpper);

	// At a capital after a lower-case letter, or at the last capital of a run a lower-case letter follows: RotateAPIKeys
	private static bool StartsWord(string text, int index) =>
		char.IsUpper(text[index]) &&
		(char.IsLower(text[index - 1]) || (index + 1 < text.Length && char.IsLower(text[index + 1])));

	private static string Words(string pascalCase)
	{
		List<string> words = [];
		int start = 0;
		for (int index = 1; index <= pascalCase.Length; index++)
		{
			if (index == pascalCase.Length || StartsWord(pascalCase, index))
			{
				words.Add(pascalCase[start..index]);
				start = index;
			}
		}

		return string.Join(
			' ',
			words.Select((word, position) => position == 0 || IsAcronym(word) ? word : word.ToLowerInvariant()));
	}
}