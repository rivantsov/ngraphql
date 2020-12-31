using System.Collections.Generic;
using System.Linq;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Introspection;

namespace NGraphQL.Model.Construction {

  public class IntrospectionSchemaBuilder {
    GraphQLApiModel _model;
    __Schema _schema;
    TypeRef _stringNotNull;

    public __Schema Build(GraphQLApiModel model) {
      _model = model;
      _stringNotNull = _model.GetScalarTypeDef("String").TypeRefNotNull;
      _schema = _model.Schema_ = new __Schema();

      // Create type objects without internal details; for typeDef and its typeRefs
      foreach (var typeDef in _model.Types) {
        if (!typeDef.IsSchemaType())
          continue; 
        CreateTypeObject(typeDef);
        foreach(var typeRef in typeDef.TypeRefs)
          SetupTypeObject(typeRef); 
      }

      // Build types internals - fields, etc
      BuildTypeObjectsInternals();
      BuildDirectives(); 
      CompleteSchemaObject(); 
      return _schema;
    }

    private void CompleteSchemaObject() {
      _schema.QueryType = _model.QueryType.Type_; 
      if (_model.MutationType != null)
        _schema.MutationType = _model.MutationType.Type_;
      if (_model.SubscriptionType != null)
        _schema.SubscriptionType = _model.SubscriptionType.Type_;
    }

    private void BuildDirectives() {
      foreach(var dirDef in _model.Directives.Values) {
        var dirInfo = dirDef.DirInfo;
        var dirAttr = dirDef.DeprecatedAttribute;
        var deprDescr = dirAttr?.ArgValues[0] as string; 
        var dir_ = new __Directive() {
           Name = dirDef.Name, Description = dirDef.Description, Locations = dirInfo.Locations, 
          IsDeprecated = dirAttr != null, DeprecationReason = deprDescr
        };
        if (dirDef.Args != null)
          dir_.Args =
            dirDef.Args.Select(ivd => new __InputValue() {
              Name = ivd.Name, Description = ivd.Description,
              Type = ivd.TypeRef.Type_, DefaultValue = ivd.DefaultValue + string.Empty
            }).ToArray();
        _schema.Directives.Add(dir_); 
      }
    }

    private void CreateTypeObject(TypeDefBase typeDef) {
      var type_ = new __Type() {
        Name = typeDef.Name, Kind = typeDef.Kind, Description = typeDef.Description, DisplayName = typeDef.Name,
      };
      typeDef.Intro_ = type_;
      _schema.Types.Add(type_);

      // Initialize lists - we do this for all types upfront to allow Build methods to access other type's lists
      //  without worrying if it is created or not. For ex, BuildObjectType finds interfaces and adds itself 
      //  to PossibleTypes list of each interface it implements
      switch(type_.Kind) {

        case TypeKind.Object: 
          type_.Fields = new List<__Field>();
          type_.Interfaces = new List<__Type>();
          break;

        case TypeKind.Interface:
          type_.Fields = new List<__Field>();
          type_.PossibleTypes = new List<__Type>();
          break;

        case TypeKind.InputObject:
          type_.InputFields = new List<__InputValue>();
          break;

        case TypeKind.Enum:
          type_.EnumValues = new List<__EnumValue>();
          break; 

        case TypeKind.Union:
          type_.PossibleTypes = new List<__Type>();
          break;
      }
    }

    private void BuildTypeObjectsInternals() {
      foreach(var typeDef in _model.Types) {
        if (typeDef.Type_ == null) //not schema type
          continue; 
        switch(typeDef) {
          case ScalarTypeDef std:
            BuildScalarType(std);
            break;

          case ObjectTypeDef otd:
            BuildObjectType(otd);
            AddTypeNameField(otd); 
            break;

          case InterfaceTypeDef intDef:
            BuildInterfaceType(intDef);
            AddTypeNameField(intDef); 
            break;

          case InputObjectTypeDef inpDef:
            BuildInputType(inpDef);
            break;

          case EnumTypeDef etd:
            BuildEnumType(etd);
            break;

          case UnionTypeDef utd:
            BuildUnionType(utd);
            break;
        } //switch
      }
    } //method

    private void AddTypeNameField(ComplexTypeDef typeDef) {
      var fld = new 
        FieldDef("__typename", _stringNotNull) { Reader = t => typeDef.Name  };
      fld.Flags |= FieldFlags.Hidden;
      typeDef.Fields.Add(fld); 
    }

    private void BuildScalarType(ScalarTypeDef inpTypeDef) {
      // nothing to do, everything is assigned already.
    }

    private void BuildObjectType(ObjectTypeDef objTypeDef) {
      var type_ = objTypeDef.Type_; 
      // build fields
      foreach(var fld in objTypeDef.Fields) {
        var fld_ = new __Field() { Name = fld.Name, Description = fld.Description, Type = fld.TypeRef.Type_ };
        // convert args
        fld_.Args = 
          fld.Args.Select(ivd => new __InputValue() { 
                        Name = ivd.Name, Description = ivd.Description, 
                        Type = ivd.TypeRef.Type_, DefaultValue = ivd.DefaultValue + string.Empty
                      })
                  .ToArray();
        type_.Fields.Add(fld_);
      } //foreach fld
      // Interfaces
      foreach(var intfDef in objTypeDef.Implements) {
        var intf_ = intfDef.Type_;
        type_.Interfaces.Add( intf_);
        intf_.PossibleTypes.Add(type_);
      }
    }

    private void BuildInterfaceType(InterfaceTypeDef intfTypeDef) {
      var type_ = intfTypeDef.Type_; 
      type_.Fields = new List<__Field>();
      type_.PossibleTypes = new List<__Type>();

      // build fields
      foreach(var fld in intfTypeDef.Fields) {
        var fld_ = new __Field() {
          Name = fld.Name, Description = fld.Description, Type = fld.TypeRef.Type_
        };
        // convert args
        fld_.Args =
          fld.Args.Select(ivd => new __InputValue() {
            Name = ivd.Name, Description = ivd.Description,
            Type = ivd.TypeRef.Type_,  DefaultValue = ivd.DefaultValue + string.Empty
          })
          .ToArray();
        type_.Fields.Add(fld_);
      } //foreach fld
      // PossibleTypes in each interface are taken care of by object types implementing the interface
    }

    private void BuildInputType(InputObjectTypeDef inpTypeDef) {
      var type_ = inpTypeDef.Type_; 
      foreach(var inpFldDef in inpTypeDef.Fields) {
        var inp_ = new __InputValue() {
          Name = inpFldDef.Name, Description = inpFldDef.Description,
          DefaultValue = inpFldDef.HasDefaultValue ? inpFldDef.DefaultValue + string.Empty : null,
          Type = inpFldDef.TypeRef.Type_
        };
        type_.InputFields.Add(inp_);
      }
    }

    private void BuildEnumType(EnumTypeDef enumTypeDef) {
      var type_ = enumTypeDef.Type_; 
      foreach(var enumV in enumTypeDef.EnumValues) {
        var enumV_ = new __EnumValue() {
          Name = enumV.Name, Description = enumV.Description,
        };
        type_.EnumValues.Add(enumV_);
      }
    }

    private void BuildUnionType(UnionTypeDef unionTypeDef) {
      var type_ = unionTypeDef.Type_;
      foreach(var t in unionTypeDef.PossibleTypes)
        type_.PossibleTypes.Add(t.Type_);
    }

    private void SetupTypeObject(TypeRef typeRef) {
      if(typeRef.Type_ != null)
        return;
      var parent = typeRef.Parent;
      if (parent == null) { //this is TypeDef.TypeRefNull value, should have same Type_ as typeDef
        typeRef.Type_ = typeRef.TypeDef.Type_;
        return; 
      }
      // Make sure parent has it set
      if(parent.Type_ == null)
        SetupTypeObject(parent);
      switch(typeRef.Kind) {
        case TypeKind.NotNull:
        case TypeKind.List:
          typeRef.Type_ = new __Type() { Kind = typeRef.Kind, OfType = parent.Type_, DisplayName = typeRef.Name };
          return; 
      }
    }

  }
}
