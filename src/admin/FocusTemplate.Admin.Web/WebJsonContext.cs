using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web;

[JsonSerializable(typeof(RequestDiagnostics))]
[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class WebJsonContext : JsonSerializerContext;