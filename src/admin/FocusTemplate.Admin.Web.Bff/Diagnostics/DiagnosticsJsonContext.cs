using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared.Diagnostics;

namespace FocusTemplate.Admin.Web.Bff.Diagnostics;

[JsonSerializable(typeof(RequestDiagnostics))]
internal sealed partial class DiagnosticsJsonContext : JsonSerializerContext;