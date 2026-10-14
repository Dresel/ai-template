using FluentValidation;

namespace SpatialFocus.Web.Foundation.Validation.Core;

public sealed record FieldMessage(string Text, Severity Severity, string? Code);