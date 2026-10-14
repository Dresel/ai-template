using System.Text.Json.Serialization;
using SpatialFocus.Web.Bff.Shared;

namespace SpatialFocus.Web.Hosting.Authentication;

[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class AuthenticationJsonContext : JsonSerializerContext;