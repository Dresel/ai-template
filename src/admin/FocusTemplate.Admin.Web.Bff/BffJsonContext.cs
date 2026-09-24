using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web.Bff;

[JsonSerializable(typeof(ClientConfiguration))]
[JsonSerializable(typeof(RequestDiagnostics))]
[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class BffJsonContext : JsonSerializerContext;