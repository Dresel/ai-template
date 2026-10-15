using SpatialFocus.Testing.Api;

namespace SpatialFocus.Testing.Host;

public abstract class ApiTestBase(ApiFixture factory) : ApiTestBase<ApiFixture>(factory);