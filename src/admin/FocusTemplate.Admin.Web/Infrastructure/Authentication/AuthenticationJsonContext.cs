using System.Text.Json.Serialization;
using FocusTemplate.Admin.Shared;

namespace FocusTemplate.Admin.Web.Infrastructure.Authentication;

[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class AuthenticationJsonContext : JsonSerializerContext;