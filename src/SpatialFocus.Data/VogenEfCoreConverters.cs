using SpatialFocus.Primitives;
using Vogen;

namespace SpatialFocus.Data;

// Converters are generated here because SpatialFocus.Primitives cannot reference EF Core. UserId comes with the
// foundation. AppDbContextBase.ConfigureIds registers them via RegisterAllInVogenEfCoreConverters.
[EfCoreConverter<GroupId>]
[EfCoreConverter<Permission>]
internal sealed partial class VogenEfCoreConverters;