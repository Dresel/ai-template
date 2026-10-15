using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace SpatialFocus.Testing;

// For the registrations that need to know where the app runs, which a test without a WebAssembly host has to state
public sealed class TestHostEnvironment : IWebAssemblyHostEnvironment
{
	public string BaseAddress => "http://localhost/";

	public string Environment => Environments.Development;
}