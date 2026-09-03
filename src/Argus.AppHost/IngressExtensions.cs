namespace Argus.AppHost;

internal static class IngressExtensions
{
	public static IResourceBuilder<ProjectResource> WithTlsOffloadingIngress(
		this IResourceBuilder<ProjectResource> app,
		string endpointName = "http")
	{
		app.WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

		EndpointReference upstream = app.GetEndpoint(endpointName);

#pragma warning disable ASPIRECERTIFICATES001
		IResourceBuilder<ContainerResource> ingress = app.ApplicationBuilder
			.AddContainer($"{app.Resource.Name}-ingress", "nginx")
			.WithBindMount("nginx/default.conf.template", "/etc/nginx/templates/default.conf.template", true)
			.WithEnvironment("APP_HOST", upstream.Property(EndpointProperty.Host))
			.WithEnvironment("APP_PORT", upstream.Property(EndpointProperty.Port))
			.WithHttpsEndpoint(targetPort: 443, name: "https")
			.WithExternalHttpEndpoints()
			.WithHttpsDeveloperCertificate()
			.WithHttpsCertificateConfiguration(ctx =>
			{
				ctx.EnvironmentVariables["SSL_CERT_PATH"] = ctx.CertificatePath;
				ctx.EnvironmentVariables["SSL_KEY_PATH"] = ctx.KeyPath;

				return Task.CompletedTask;
			})
			.WaitFor(app);
#pragma warning restore ASPIRECERTIFICATES001

		return app;
	}
}