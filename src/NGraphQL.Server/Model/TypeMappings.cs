using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {

  public class TypeMapping {
    public EntityMapping SourceEntityMapping;
    // accessed by field index
    public List<FieldMapping> Fields = new List<FieldMapping>();   
  }

  public class FieldMapping {
    public TypeMapping TypeMapping; 
    public FieldDef Field;
    public ResolverMethodInfo ResolverInfo; 
    public Func<object, object> Reader; 
  }
  
}
