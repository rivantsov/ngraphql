using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Core.Scalars;
using NGraphQL.Model;
using NGraphQL.Introspection;
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.Model {

  public class TypeDefBase : GraphQLModelObject {
    public readonly GraphQLModule Module;
    public IList<Attribute> Attributes; 
    public TypeKind Kind;
    public Type ClrType;
    public bool Hidden;
    public bool IsDefaultForClrType = true; // false for ID type, to skip registration

    public __Type Type_ => (__Type)Intro_; 
    // Default type refs
    public readonly TypeRef TypeRefNull;
    public readonly TypeRef TypeRefNotNull;
    public readonly IList<TypeRef> TypeRefs = new List<TypeRef>();

    public TypeDefBase(string name, TypeKind kind, Type clrType, IList<Attribute> attributes, GraphQLModule module) {
      Name = name;
      Kind = kind;
      ClrType = clrType;
      Attributes = attributes; 
      Module = module; 
      TypeRefNull = new TypeRef(this);
      TypeRefNotNull = new TypeRef(TypeRefNull, TypeKind.NonNull);
      TypeRefs.Add(TypeRefNull);
      TypeRefs.Add(TypeRefNotNull);
    }

    public virtual object ToOutput(FieldContext context, object value) {
      return value;
    }

    // used in Schema doc output
    public virtual string ToSchemaDocString(object value) {
      if(value == null)
        return "null";
      return value.ToString();
    }

    public override string ToString() => $"{Name}/{Kind}";
    public virtual void Init(GraphQLServer server) { }
  }

  public class ModelDirective {
    public DirectiveDef Def;
    public DirectiveLocation Location; 
    public BaseDirectiveAttribute ModelAttribute; 

    public override string ToString() {
      return Def?.ToString(); 
    }
    public static IList<ModelDirective> EmptyList = new ModelDirective[] { }; 
  }

  public class ScalarTypeDef : TypeDefBase {
    public readonly Scalar Scalar;
    public ScalarTypeDef(Scalar scalar, GraphQLModule module) 
            : base (scalar.Name, TypeKind.Scalar, scalar.DefaultClrType, EmptyAttributeList, module) {
      Scalar = scalar;
      base.IsDefaultForClrType = Scalar.IsDefaultForClrType; 
    }

    public override string ToSchemaDocString(object value) {
      return this.Scalar.ToSchemaDocString(value); 
    }
  }

  // base for Interface and Object types
  public abstract class ComplexTypeDef : TypeDefBase {
    public TypeRole TypeRole;
    public List<FieldDef> Fields = new List<FieldDef>();
    public ComplexTypeDef(string name, TypeKind kind, Type clrType, IList<Attribute> attrs, GraphQLModule module, 
         TypeRole typeRole = TypeRole.Data) 
       : base(name, kind, clrType, attrs, module) {
      this.TypeRole = typeRole;
    }
  }

  public class ObjectTypeDef : ComplexTypeDef {
    public List<InterfaceTypeDef> Implements = new List<InterfaceTypeDef>();
    public List<ObjectTypeMappingExt> Mappings = new List<ObjectTypeMappingExt>();

    public ObjectTypeDef(string name, Type clrType, IList<Attribute> attrs, GraphQLModule module, 
          TypeRole typeRole = TypeRole.Data) 
             : base(name, TypeKind.Object, clrType, attrs, module, typeRole) {  }
  }

  public class InterfaceTypeDef : ComplexTypeDef {
    public List<ObjectTypeDef> PossibleTypes = new List<ObjectTypeDef>();

    public InterfaceTypeDef(string name, Type clrType, IList<Attribute> attrs, GraphQLModule module) 
      : base(name, TypeKind.Interface, clrType, attrs, module) { }
    
  }

  public class UnionTypeDef : TypeDefBase {
    public List<ObjectTypeDef> PossibleTypes = new List<ObjectTypeDef>();

    public UnionTypeDef(string name, Type clrType, IList<Attribute> attrs, GraphQLModule module) 
        : base(name, TypeKind.Union, clrType, attrs, module) { }
  }

  public class InputObjectTypeDef : TypeDefBase {
    public List<InputValueDef> Fields = new List<InputValueDef>();

    public InputObjectTypeDef(string name, Type clrType, IList<Attribute> attrs, GraphQLModule module) 
        : base(name, TypeKind.InputObject, clrType, attrs, module) { }

  }

  // Arg or InputObject field
  public class InputValueDef : GraphQLModelObject {
    public TypeRef TypeRef;
    public IList<Attribute> Attributes = EmptyAttributeList; 

    public bool HasDefaultValue;
    public object DefaultValue;

    public Type ParamType; // Arg only; exact resolver parameter type
    public MemberInfo InputObjectClrMember; // InputObject only

    public InputValueDef() { }
    public override string ToString() => $"{Name}/{TypeRef}";
    public static IList<InputValueDef> EmptyList = new InputValueDef[] { };
  }

  [DisplayName("{Name}/{TypeRef.Name}")]
  public class FieldDef : GraphQLModelObject {
    public readonly ComplexTypeDef OwnerType; 
    public TypeRef TypeRef;
    public IList<Attribute> Attributes;

    public FieldFlags Flags;
    public IList<InputValueDef> Args = new List<InputValueDef>();
    public MemberInfo ClrMember;
    /* //moved these to ObjectFieldMapping
    public ResolverMethodInfo Resolver;
    public Func<object, object> Reader;
    public FieldExecutionType ExecutionType;
    */

    public FieldDef(ComplexTypeDef ownerType, string name, TypeRef typeRef) {
      OwnerType = ownerType; 
      Name = name;
      TypeRef = typeRef;
      if (typeRef.TypeDef.IsComplexReturnType()) {
        Flags |= FieldFlags.ReturnsComplexType;
      }
    }
  }

  public class DirectiveDef : GraphQLModelObject {
    public DirectiveRegistration DirInfo;
    public DeprecatedDirAttribute DeprecatedAttribute; //if marked
    public IDirectiveHandler Handler; 
    public IList<InputValueDef> Args = InputValueDef.EmptyList;
    public DirectiveDef() { }

    public static IList<DirectiveDef> EmptyList = new DirectiveDef[] { }; 
  }

  [DisplayName("{Method.Name}")]
  public class ResolverMethodInfo {
    public MethodInfo Method;
    public Attribute SourceAttribute;
    public Type ResolverClass; 

    public bool ReturnsTask;
    public Func<object, object> TaskResultReader; // reads Task<T>.Result

    public override string ToString() => $"{Method.Name}";
  }

  [DisplayName("{Type}")]
  public class ResolverClassInfo {
    public GraphQLModule Module;
    public Type Type; 
  }

  [DisplayName("{Name}/{Kind}")]
  public class TypeRef { 
    public readonly TypeDefBase TypeDef;
    public readonly TypeKind Kind;
    public readonly TypeRef Inner;
    public __Type Type_; //note: not the same as this.TypeDef.Type_ 
    // list of all kinds in parents starting from original type def. 
    public readonly IList<TypeKind> KindsPath;
    public readonly string Name;
    public readonly int Rank;
    public readonly bool IsList;
    public bool IsNotNull => Kind == TypeKind.NonNull;
    public Type ClrType;

    public TypeRef(TypeDefBase typeDef) {
      TypeDef = typeDef;
      Kind = TypeDef.Kind;
      Name = this.GetTypeRefName();
      KindsPath = new[] { Kind };
      ClrType = this.GetClrType(); 
    }

    public TypeRef(TypeRef parent, TypeKind kind) {
      Inner = parent;
      Kind = kind;
      TypeDef = parent.TypeDef;
      KindsPath = new List<TypeKind>(parent.KindsPath);
      KindsPath.Add(kind); 
      // for this constructor we should have only List or NotNull kinds
      switch(Kind) {
        case TypeKind.NonNull:
          Rank = parent.Rank;
          break;
        case TypeKind.List:
          Rank = parent.Rank + 1;
          break;
        default:
          throw new Exception($"FATAL: Invalid type kind '{kind}', expected List or NotNull."); // should never happen
      }
      Name = this.GetTypeRefName();
      IsList = kind == TypeKind.List || parent.Kind == TypeKind.List;
      ClrType = this.GetClrType(); 
    }

    public override string ToString() => Name;

    public override bool Equals(object obj) {
      var other = obj as TypeRef;
      if(other == null) return false;
      return Name == other.Name; 
    }

    public override int GetHashCode() => Name.GetHashCode();
  }

}
