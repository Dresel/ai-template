using FluentValidation;

namespace FocusTemplate.Admin.Web.Foundation.Validation.Core;

public sealed record FieldMessage(string Text, Severity Severity, string? Code);