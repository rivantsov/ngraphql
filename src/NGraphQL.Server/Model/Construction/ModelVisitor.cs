using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model.Construction {
  // Currently not used, might be used by custom implementations
  public class ModelVisitor {
    GraphQLApiModel _model; 

    public ModelVisitor(GraphQLApiModel model) {
      _model = model; 
    }

    public void Visit(Action<GraphQLModelObject> action) {
      foreach (var dirDef in _model.Directives.Values)
        Visit(dirDef, action);
      foreach (var typeDef in _model.Types) {
        if (!typeDef.IsDataType()) // skip utility types like Query, Mutation etc 
          continue;
        Visit(typeDef, action);
      }
    } //method

    private void Visit(GraphQLModelObject modelObj, Action<GraphQLModelObject> action) {
      action(modelObj);
      switch (modelObj) {
        case ComplexTypeDef ctd: // object type and interface type
          foreach (var fld in ctd.Fields)
            Visit(fld, action);
          break;

        case InputObjectTypeDef itd:
          foreach (var f in itd.Fields)
            Visit(f, action);
          break;

        case EnumTypeDef etd:
          foreach (var enumFld in etd.Fields)
            Visit(enumFld, action);
          break;

        case ScalarTypeDef _:
        case UnionTypeDef _:
        case InputValueDef _:
          // nothing to do
          break;

        case FieldDef fd:
          if (fd.Args != null)
            foreach (var a in fd.Args)
              Visit(a, action);
          break;

        case DirectiveDef dirDef:
          if (dirDef.Args != null)
            foreach (var a in dirDef.Args)
              Visit(a, action);
          break;
      } //switch
    }


  }
}
