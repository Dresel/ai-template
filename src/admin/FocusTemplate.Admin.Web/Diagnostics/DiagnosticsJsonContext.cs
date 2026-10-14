using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared.Diagnostics;

namespace FocusTemplate.Admin.Web.Diagnostics;

[JsonSerializable(typeof(RequestDiagnostics))]
internal sealed partial class DiagnosticsJsonContext : JsonSerializerContext;