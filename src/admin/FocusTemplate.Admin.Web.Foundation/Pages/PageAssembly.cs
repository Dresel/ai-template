using System.Reflection;

namespace FocusTemplate.Admin.Web.Foundation.Pages;

// An assembly whose routable pages the host's Router adds, registered by the feature that ships them
public sealed record PageAssembly(Assembly Assembly);