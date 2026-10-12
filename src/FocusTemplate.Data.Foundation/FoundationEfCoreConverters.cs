using FocusTemplate.Primitives;
using Vogen;

namespace FocusTemplate.Data.Foundation;

// The ids every model holds through the audit columns. A context registers them with AddFoundationConventions, and its
// own ids with converters of its own.
[EfCoreConverter<UserId>]
internal sealed partial class FoundationEfCoreConverters;