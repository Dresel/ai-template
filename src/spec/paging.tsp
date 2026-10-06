// Spread into a list's request (...PageQuery), so the request carries skip and top as members of its own and this
// model generates nothing. The names follow OData and Radzen's LoadDataArgs, which hands a grid's page over as Skip/Top.
namespace FocusTemplate.Paging;

/** Which page of a list: how many items to skip, how many to take. */
model PageQuery {
  /** Items to skip. */
  @minValue(0)
  skip?: int32 = 0;

  /** Items per page, at most 100. */
  @minValue(1)
  @maxValue(100)
  top?: int32 = 10;
}
