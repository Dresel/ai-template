using System.Text.Json.Serialization;
using FocusTemplate.Web.Bff.Shared;

namespace FocusTemplate.Web.Bff.Hosting;

[JsonSerializable(typeof(ClientConfiguration))]
[JsonSerializable(typeof(RequestDiagnostics))]
[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class BffJsonContext : JsonSerializerContext;