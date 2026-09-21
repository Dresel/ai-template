using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web;

[JsonSerializable(typeof(RequestDiagnostics))]
internal sealed partial class WebJsonContext : JsonSerializerContext;