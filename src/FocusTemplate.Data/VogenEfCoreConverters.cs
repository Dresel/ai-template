using FocusTemplate.Primitives;
using Vogen;

namespace FocusTemplate.Data;

// Converters are generated here because FocusTemplate.Primitives cannot reference EF Core. UserId comes with the
// foundation. AppDbContextBase.ConfigureIds registers them via RegisterAllInVogenEfCoreConverters.
[EfCoreConverter<GroupId>]
[EfCoreConverter<Permission>]
internal sealed partial class VogenEfCoreConverters;