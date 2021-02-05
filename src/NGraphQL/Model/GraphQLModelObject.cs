using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Introspection;

namespace NGraphQL.Model {

  public class GraphQLModelObject {
    public string Name { get; set; }
    public string Description;
    // It should be IList<ModelDirective>, but ModelDirective is not available here, 
    //  so we use list of objects
    public IList<object> Directives;
    public IntroObjectBase Intro_;

    public override string ToString() => Name;
    public static readonly IList<Attribute> EmptyAttributeList = new Attribute[] { };
  }

}
