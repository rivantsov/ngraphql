using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Introspection;
using NGraphQL.Model.Introspection;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Model {

  public class GraphQLModelObject {
    public string Name { get; set; }
    public string Description;
    public override string ToString() => Name;
  }

  public class TypeDefBase : GraphQLModelObject {
    public GraphQLModule Module;
    public TypeRole TypeRole;

    public TypeKind Kind;
    public Type ClrType;
    public bool Hidden;
    public IList<Directive> Directives = Directive.EmptyList;
    public bool IsDefaultForClrType = true; // false for ID type, to skip registration

    public __Type Type_; // Introspection object
    // Default type refs
    public readonly TypeRef TypeRefNull;
    public readonly TypeRef TypeRefNotNull;
    public readonly IList<TypeRef> TypeRefs = new List<TypeRef>(); 

    public TypeDefBase(string name, TypeKind kind, Type clrType) {
      Name = name;
      Kind = kind;
      ClrType = clrType;
      TypeRefNull = new TypeRef(this);
      TypeRefNotNull = new TypeRef(TypeRefNull, TypeKind.NotNull);
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

  public class ScalarTypeDef : TypeDefBase {
    public readonly Scalar Scalar;
    public ScalarTypeDef(Scalar scalar) : base (scalar.Name, TypeKind.Scalar, scalar.DefaultClrType) {
      Scalar = scalar; 
    }
  }

  // base for Interface and Object types
  public abstract class ComplexTypeDef : TypeDefBase {
    public List<FieldDef> Fields = new List<FieldDef>();
    public ComplexTypeDef(string name, TypeKind kind, Type clrType) : base(name, kind, clrType) { }
  }

  public class ObjectTypeDef : ComplexTypeDef {
    public List<InterfaceTypeDef> Implements = new List<InterfaceTypeDef>();
    public EntityMapping Mapping;

    public ObjectTypeDef(string name, Type clrType) : base(name, TypeKind.Object, clrType) { }
  }

  public class InterfaceTypeDef : ComplexTypeDef {
    public List<ObjectTypeDef> PossibleTypes = new List<ObjectTypeDef>();

    public InterfaceTypeDef(string name, Type clrType) : base(name, TypeKind.Interface, clrType) { }
    
  }

  public class UnionTypeDef : TypeDefBase {
    public List<ObjectTypeDef> PossibleTypes = new List<ObjectTypeDef>();

    public UnionTypeDef(string name, Type clrType) : base(name, TypeKind.Union, clrType) { }
  }

  public class InputObjectTypeDef : TypeDefBase {
    public List<InputValueDef> Fields = new List<InputValueDef>();

    public InputObjectTypeDef(string name, Type clrType) : base(name, TypeKind.InputObject, clrType) { }

  }

  // Arg or InputObject field
  public class InputValueDef : GraphQLModelObject {
    public TypeRef TypeRef;
    public bool HasDefaultValue;
    public object DefaultValue;
    public IList<Directive> Directives;

    public Type ParamType; // Arg only; exact resolver parameter type
    public MemberInfo InputObjectClrMember; // inputobject only

    public InputValueDef() { }
    public override string ToString() => $"{Name}/{TypeRef}";
  }

  [DisplayName("{Name}/{TypeRef.Name}")]
  public class FieldDef : GraphQLModelObject {
    public TypeRef TypeRef; 

    public FieldFlags Flags;
    public IList<InputValueDef> Args = new List<InputValueDef>();
    public IList<Directive> Directives;
    public MemberInfo ClrMember;
    public ResolverMethodInfo Resolver;
    public Func<object, object> Reader;
    public FieldExecutionType ExecutionType;

    public FieldDef(string name, TypeRef typeRef) {
      Name = name;
      TypeRef = typeRef;
      if (typeRef.TypeDef.IsComplexReturnType()) {
        Flags |= FieldFlags.ReturnsComplexType;
      }
    }
  }

  [DisplayName("{Method.Name}")]
  public class ResolverMethodInfo {
    public MethodInfo Method;
    public ResolverAttribute Attribute;
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

  public class EntityMapping {
    public Type GraphQLType;
    public Type EntityType;
    public LambdaExpression Expression;
    internal EntityMapping() { }
  }

  public class EntityMapping<TEntity>: EntityMapping {
    internal EntityMapping() {
      base.EntityType = typeof(TEntity);
    }
    public void To<TGraphQL>(Expression<Func<TEntity, TGraphQL>> expression = null) where TGraphQL: class {
      GraphQLType = typeof(TGraphQL);
      Expression = expression;
    }
    public void ToUnion<TUnion>() where TUnion: UnionBase {
      GraphQLType = typeof(TUnion);
    }
  }

  [DisplayName("{Name}/{Kind}")]
  public class TypeRef { 
    public readonly TypeDefBase TypeDef;
    public readonly TypeKind Kind;
    public readonly TypeRef Parent;
    // list of all kinds in parents starting from original type def. 
    public readonly IList<TypeKind> KindsPath;
    public readonly string Name;
    public __Type Type_;
    public readonly int Rank;
    public readonly bool IsList;
    public bool IsNotNull => Kind == TypeKind.NotNull;
    public Type ClrType;

    public TypeRef(TypeDefBase typeDef) {
      TypeDef = typeDef;
      Kind = TypeDef.Kind;
      Name = this.GetTypeRefName();
      KindsPath = new[] { Kind };
      ClrType = this.GetClrType(); 
    }

    public TypeRef(TypeRef parent, TypeKind kind) {
      Parent = parent;
      Kind = kind;
      TypeDef = parent.TypeDef;
      KindsPath = new List<TypeKind>(parent.KindsPath);
      KindsPath.Add(kind); 
      // for this constructor we should have only List or NotNull kinds
      switch(Kind) {
        case TypeKind.NotNull:
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

  public class DirectiveDef : GraphQLModelObject {
    public IList<InputValueDef> Args;
    public DirectiveMetaDataAttribute MetaData; 
  }

  public class Directive : GraphQLModelObject {
    public static IList<Directive> EmptyList = new Directive[] { };
    public DirectiveDef Def;
    public object[] ArgValues;

    public Directive() { }
  }


}
