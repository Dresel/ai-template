using System.Linq.Expressions;

namespace FocusTemplate.Admin.Web.Foundation.Validation.Core;

public static class FieldPath
{
	// The members after the captured variable. Only names are read, the view model is never reflected over
	public static string Of<TValue>(Expression<Func<TValue>> value) => Walk(value.Body);

	// The same for a validator's expressions, which start from the model: form => form.Address.Street
	public static string Of<TModel, TValue>(Expression<Func<TModel, TValue>> value) => Walk(value.Body);

	// A member or item of the parent: Address.Street beneath Address, Tags[1] beneath Tags
	public static bool IsBeneath(string path, string parent) =>
		path.Length > parent.Length &&
		path.StartsWith(parent, StringComparison.OrdinalIgnoreCase) &&
		path[parent.Length] is '.' or '[';

	// The same field, or one within the other: the rules of either can concern the other
	public static bool Overlap(string path, string other) =>
		path == other || IsBeneath(path, other) || IsBeneath(other, path);

	private static string Walk(Expression body)
	{
		Stack<string> members = [];
		Expression? node = body is UnaryExpression { NodeType: ExpressionType.Convert, } convert ? convert.Operand : body;

		while (node is MemberExpression { Expression: not (null or ConstantExpression), } member)
		{
			members.Push(member.Member.Name);
			node = member.Expression;
		}

		return string.Join('.', members);
	}
}