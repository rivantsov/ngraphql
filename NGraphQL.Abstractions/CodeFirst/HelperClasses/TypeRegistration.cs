using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.CodeFirst {
  public enum TypeRole {
    Query,
    Mutation,
    Subscription,
    Schema,
    Enum,
    Object,
    Input,
    Interface,
    Union,
  }

  public class TypeRegistration {
    public TypeRole Role;
    public Type Type;
    public string GraphQLName; // optional
  }
}
