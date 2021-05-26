using System;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Scalars;
using NGraphQL.Core;
using NGraphQL.Introspection;
using NGraphQL.Model;

namespace NGraphQL.Core {

  /// <summary>Core module defines standard and custom scalars; @include and @skip directives.</summary>
  public class CoreModule : GraphQLModule {

    public CoreModule() {
      this.ScalarTypes.AddRange(new Type[] {
        typeof(StringScalar), typeof(IntScalar), typeof(LongScalar), typeof(FloatScalar), typeof(DoubleScalar),
        typeof(BooleanScalar), typeof(IdScalar), typeof(DateTimeScalar), typeof(DateScalar), typeof(TimeScalar),
        typeof(UuidScalar), typeof(DecimalScalar)
      });
      // Directives 
      this.RegisterDirective("deprecated", typeof(DeprecatedDirAttribute),
          DirectiveLocation.TypeSystemLocations, "Marks type system element as deprecated",
          handlerType: typeof(DeprecatedDirectiveHandler), listInSchema: false);
      this.RegisterDirective("include", nameof(IncludeSkipSignature),
          DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment, 
          "Conditional field include.", 
          handlerType: typeof(IncludeDirectiveHandler) , listInSchema: false);
      this.RegisterDirective("skip", nameof(IncludeSkipSignature),
          DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
          "Conditional field skip.", 
          handlerType: typeof(SkipDirectiveHandler), listInSchema: false);
    }

    internal static void DeprecatedSignature(string reason) { }
    internal static void IncludeSkipSignature(bool @if) { }
  }
}
