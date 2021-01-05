using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Server.Http {

  public enum HttpContentType {
    None,
    Json,
    GraphQL,
  }

  [Flags]
  public enum GraphQLHttpOptions {
    None = 0,
    ReturnExceptionDetails = 1,
    SuppressChunking = 1 << 1,
  }
}
