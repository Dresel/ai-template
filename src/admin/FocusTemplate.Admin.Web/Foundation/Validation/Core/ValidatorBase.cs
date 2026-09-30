using System.Linq.Expressions;
using FluentValidation;

namespace FocusTemplate.Admin.Web.Foundation.Validation.Core;

public abstract class ValidatorBase<TModel> : AbstractValidator<TModel>
{
	private readonly Dictionary<string, IReadOnlyList<string>> dependencies = [with(StringComparer.Ordinal)];

	// By the path of a field: the paths of the fields to validate with it
	public IReadOnlyDictionary<string, IReadOnlyList<string>> Dependencies => this.dependencies;

	// Declared next to the rule that reads the other field, so the two are removed together
	protected void DependsOn<TValue, TOther>(Expression<Func<TModel, TValue>> field, Expression<Func<TModel, TOther>> on)
	{
		string path = FieldPath.Of(on);
		this.dependencies[path] = [.. this.dependencies.GetValueOrDefault(path, []), FieldPath.Of(field),];
	}
}