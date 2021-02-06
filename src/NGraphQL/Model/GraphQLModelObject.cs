using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.Introspection;

namespace NGraphQL.Model {

  /// <summary>Base for all GraphQL Model classes. </summary>
  /// <remarks>Most of the model classes are in NGraphQL.Server. We need this base class here because <see cref="EnumHandler"/>
  /// class is also part of the model. </remarks>
  public class GraphQLModelObject {
    public string Name { get; set; }
    public string Description;
    // It should be IList<ModelDirective>, but ModelDirective is not available in this assembly, 
    //  so we use list of objects
    public IList<object> Directives;
    public IntroObjectBase Intro_;

    public override string ToString() => Name;
    public static readonly IList<Attribute> EmptyAttributeList = new Attribute[] { };
  }

}
