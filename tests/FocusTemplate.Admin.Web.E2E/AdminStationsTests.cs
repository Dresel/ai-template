using SpatialFocus.Weather.E2E;

namespace FocusTemplate.Admin.Web.E2E;

[Collection(AspireCollection.Name)]
public sealed class AdminStationsTests(AdminAppFixture app) : StationsTests(app);