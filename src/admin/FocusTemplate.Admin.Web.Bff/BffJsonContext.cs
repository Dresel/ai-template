using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web.Bff;

[JsonSerializable(typeof(ClientConfiguration))]
[JsonSerializable(typeof(RequestDiagnostics))]
internal sealed partial class BffJsonContext : JsonSerializerContext;