using FluentValidation.Results;

namespace FocusTemplate.Admin.Web.Foundation.Validation.Core;

// Runs the view model's rules for the fields at these paths, or for the whole form when null
public delegate Task<ValidationResult> ValidateScope(
	IReadOnlyCollection<string>? paths,
	CancellationToken cancellationToken);