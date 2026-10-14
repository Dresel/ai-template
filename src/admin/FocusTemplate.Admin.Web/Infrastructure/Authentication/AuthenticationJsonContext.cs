using System.Text.Json.Serialization;
using FocusTemplate.Web.Bff.Shared;

namespace FocusTemplate.Admin.Web.Infrastructure.Authentication;

[JsonSerializable(typeof(UserInfoResponse))]
internal sealed partial class AuthenticationJsonContext : JsonSerializerContext;