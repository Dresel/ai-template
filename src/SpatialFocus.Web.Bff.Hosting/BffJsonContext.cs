using System.Text.Json.Serialization;
using SpatialFocus.Web.Bff.Shared;

namespace SpatialFocus.Web.Bff.Hosting;

[JsonSerializable(typeof(ClientConfiguration))]
[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class BffJsonContext : JsonSerializerContext;