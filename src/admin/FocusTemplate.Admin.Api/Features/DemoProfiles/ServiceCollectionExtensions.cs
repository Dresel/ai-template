using FluentValidation;
using FocusTemplate.Admin.Shared;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FocusTemplate.Admin.Api.Features.DemoProfiles;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDemoProfiles(this IServiceCollection services) =>
		services.AddSingleton<DemoProfileCodes>()
			.Replace(ServiceDescriptor.Scoped<IValidator<DemoProfileRequest>, DemoProfileRequestValidator>());
}