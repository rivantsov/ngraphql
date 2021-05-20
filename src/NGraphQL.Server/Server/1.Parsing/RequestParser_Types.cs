using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Server.Parsing {
  using Node = Irony.Parsing.ParseTreeNode;

  partial class RequestParser {

    private TypeRef BuildTypeReference(Node typeNode) {
      return BuildTypeRefRec(typeNode);
    }

    private TypeRef BuildTypeRefRec(Node typeNode) {
      TypeRef trbase;
      switch(typeNode.Term.Name) {
        case TermNames.ListTypeRef:
          trbase = BuildTypeRefRec(typeNode.ChildNodes[0]);
          return GetCreateDerivedTypeRef(trbase, TypeKind.List);

        case TermNames.NotNullTypeRef:
          trbase = BuildTypeRefRec(typeNode.ChildNodes[0]);
          if (trbase.Kind == TypeKind.NonNull) {
            AddError($"Duplicate not-null type spec: '{typeNode.GetText()}'", typeNode);
            return trbase;
          } 
          return GetCreateDerivedTypeRef(trbase, TypeKind.NonNull);

        case TermNames.BaseTypeRef:
        default:
          var child0 = typeNode.ChildNodes[0];
          var typeDef = LookupTypeDef(child0);
          if (typeDef == null) {
            var typeName = child0.GetText();
            AddError($"Failed to match type ref '{typeName}' to existing type.", typeNode);
          }
          return typeDef.TypeRefNull;
      }
    } //method

    /* About custom type refs
      // when parsing the request, we try to lookup existing typeRef registered with TypeDef in the model; 
      // we might not find it; for ex - we are looking for type [[int]]!, but Model does not have any field or resolver arg
      // of this type. Model might have [[int]] type, and this is a legit case. We can create this new TypeRef,
      // but we cannot register it in typeDef's list - the model is read-only now.
      // So instead we add it request context CustomTypeRefs list. 
    */
    private TypeRef GetCreateDerivedTypeRef(TypeRef typeRef, TypeKind kind) {
      var kinds = new List<TypeKind>(typeRef.KindsPath);
      kinds.Add(kind);
      var existing = typeRef.TypeDef.FindTypeRef(kinds);
      if(existing != null)
        return existing;
      // try find in request
      existing = _requestContext.CustomTypeRefs.FirstOrDefault(tr => tr.TypeDef == typeRef.TypeDef && tr.KindsPath.Matches(kinds));
      if(existing != null)
        return existing;
      var newTypeRef = new TypeRef(typeRef, kind);
      _requestContext.CustomTypeRefs.Add(newTypeRef);
      return newTypeRef; 
    }



  }
}
