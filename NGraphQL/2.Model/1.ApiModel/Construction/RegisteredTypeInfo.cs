using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Construction {

  public enum RegisteredTypeCategory {
    DataType,
    Query,
    Mutation,
    Subscription,
    Resolver,
  }

  public class RegisteredTypeInfo {
    public Type ClrType;
    public GraphQLModule Module; 
    public RegisteredTypeCategory Category;
    public TypeKind? Kind;
  }
}
