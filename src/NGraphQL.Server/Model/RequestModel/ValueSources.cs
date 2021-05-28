using System.Collections.Generic;
using NGraphQL.Core.Scalars;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Model.Request {

  // equiv of Value in Gql spec
  public abstract class ValueSource : RequestObjectBase {
    public virtual bool IsConstNull() => false;
  }

  public class VariableValueSource : ValueSource {
    public string VariableName;
    public override string ToString() => VariableName;
    public VariableValueSource() { }
  }

  public class TokenValueSource : ValueSource {
    public TokenData TokenData; 
    public override string ToString() => TokenData.ParsedValue?.ToString();
    public override bool IsConstNull() => TokenData?.TermName == TermNames.NullValue;
  }

  public class ListValueSource : ValueSource {
    public ValueSource[] Values;
    public override string ToString() => $"(Count {Values?.Length})";
  }

  public class ObjectValueSource : ValueSource {
    public IDictionary<string, ValueSource> Fields = new Dictionary<string, ValueSource>();
    public override string ToString() => "(input object)";
  }

}
