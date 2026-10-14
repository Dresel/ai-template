using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace FocusTemplate.AppHost.Hosting.Ingress;

public static class ProjectResourceBuilderExtensions
{
	public static IResourceBuilder<ProjectResource> WithTlsOffloadingIngress(
		this IResourceBuilder<ProjectResource> app,
		int port,
		string endpointName = "http")
	{
		app.WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");

		EndpointReference upstream = app.GetEndpoint(endpointName);

#pragma warning disable ASPIRECERTIFICATES001
		app.ApplicationBuilder
			.AddContainer($"{app.Resource.Name}-ingress", "nginx")
			.WithContainerFiles("/etc/nginx/templates", [new ContainerFile { Name = "default.conf.template", Contents = Template(), },])
			.WithEnvironment("APP_HOST", upstream.Property(EndpointProperty.Host))
			.WithEnvironment("APP_PORT", upstream.Property(EndpointProperty.Port))
			.WithHttpsEndpoint(port: port, targetPort: 443, name: "https")
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

	// The image's entrypoint fills in the template's ${…} from the container's environment at each start
	private static string Template()
	{
		using Stream stream = typeof(ProjectResourceBuilderExtensions).Assembly.GetManifestResourceStream("default.conf.template")!;
		using StreamReader reader = new(stream);

		return reader.ReadToEnd();
	}
}