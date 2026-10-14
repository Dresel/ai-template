using System.Text.Json.Serialization;
using FocusTemplate.Web.Bff.Shared;

namespace FocusTemplate.Admin.Web.Diagnostics;

[JsonSerializable(typeof(RequestDiagnostics))]
internal sealed partial class DiagnosticsJsonContext : JsonSerializerContext;