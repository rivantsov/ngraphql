using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace StarWars.Api {

  /// <summary>Information for paginating this connection </summary>
  [GraphQLObjectType("PageInfo")]
  public class PageInfo_ {

    [Scalar("ID"), Null]
    public string StartCursor;

    [Scalar("ID"), Null]
    public string EndCursor;

    public bool HasNextPage;
  }

}
