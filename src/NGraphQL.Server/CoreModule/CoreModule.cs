using System;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Scalars;
using NGraphQL.Core;
using NGraphQL.Introspection;
using NGraphQL.Model;

namespace NGraphQL.Core {

  /// <summary>Core module defines standard and some custom scalars, and standard directives.</summary>
  public class CoreModule : GraphQLModule {

    public CoreModule() {
      this.ScalarTypes.AddRange(new Type[] {
        typeof(StringScalar), typeof(IntScalar), typeof(LongScalar), typeof(FloatScalar), typeof(DoubleScalar),
        typeof(BooleanScalar), typeof(IdScalar), typeof(DateTimeScalar), typeof(DateScalar), typeof(TimeScalar),
        typeof(UuidScalar), typeof(DecimalScalar), typeof(MapScalar), typeof(AnyScalar)
      });
      // Directives 
      this.RegisterDirective("deprecated", typeof(DeprecatedDirAttribute),
          DirectiveLocation.TypeSystemLocations, "Marks type system element as deprecated",
          handlerType: typeof(DeprecatedDirectiveHandler), listInSchema: false);
      
      this.RegisterDirective("include", nameof(IncludeSkipSignature),
          DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment, 
          "Conditional field include.", 
          handlerType: typeof(IncludeDirectiveHandler) , isCustom: false, isRepeatable: true);

      this.RegisterDirective("skip", nameof(IncludeSkipSignature),
          DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
          "Conditional field skip.", 
          handlerType: typeof(SkipDirectiveHandler), isCustom: false, isRepeatable: true);
      
      this.RegisterDirective("specifiedBy", nameof(SpecifiedBySignature),
          DirectiveLocation.Scalar,
          "Documentation link for a custom scalar.",
          handlerType: typeof(SpecifiedByDirectiveHandler), isCustom: false);
    }

    internal static void DeprecatedSignature(string reason) { }
    internal static void IncludeSkipSignature(bool @if) { }
    internal static void SpecifiedBySignature(string url) { }
  }
}
