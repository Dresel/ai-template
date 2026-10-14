using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Hosting;

namespace FocusTemplate.Web.UnitTests;

// For the registrations that need to know where the app runs, which a test without a WebAssembly host has to state
internal sealed class TestHostEnvironment : IWebAssemblyHostEnvironment
{
	public string BaseAddress => "http://localhost/";

	public string Environment => Environments.Development;
}