using System.Text.Json.Serialization;
using FocusTemplate.Web.Bff.Shared;

namespace FocusTemplate.Web.Hosting.Authentication;

[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class AuthenticationJsonContext : JsonSerializerContext;