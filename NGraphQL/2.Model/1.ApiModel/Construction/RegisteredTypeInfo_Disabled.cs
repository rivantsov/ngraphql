using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.Model.Construction {


  public class RegisteredTypeInfo {
    public Type ClrType;
    public GraphQLModule Module; 
    public ClrTypeRole TypeRole;
    public TypeKind? DataTypeKind;
    public TypeDefBase DataTypeDef;
  }

}
