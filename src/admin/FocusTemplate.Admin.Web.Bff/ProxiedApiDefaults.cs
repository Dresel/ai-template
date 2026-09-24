namespace FocusTemplate.Admin.Web.Bff;

// The contract between the WASM client's proxied HttpClients (AddProxiedHttpClient, CsrfHeaderHandler) and the BFF's YARP routes for them.
internal static class ProxiedApiDefaults
{
	public const string CsrfHeader = "X-CSRF";

	public const string Policy = "ProxiedApi";
}