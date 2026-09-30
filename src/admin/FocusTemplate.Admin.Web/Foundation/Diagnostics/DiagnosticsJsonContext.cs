using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web.Foundation.Diagnostics;

[JsonSerializable(typeof(RequestDiagnostics))]
internal sealed partial class DiagnosticsJsonContext : JsonSerializerContext;