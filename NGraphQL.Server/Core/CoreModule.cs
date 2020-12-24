using System;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Scalars;
using NGraphQL.Directives;
using NGraphQL.Introspection;

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
      this.RegisterDirective("@deprecated", typeof(DeprecatedDirAttribute),
          DirectiveLocation.TypeSystemLocations, "Marks type system element as deprecated", listInSchema: false);
      this.RegisterDirective("@include", typeof(IncludeDirective),
          DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment, 
          "Conditional field include.", listInSchema: false);
      this.RegisterDirective("@skip", typeof(SkipDirective),
          DirectiveLocation.Field | DirectiveLocation.FragmentSpread | DirectiveLocation.InlineFragment,
          "Conditional field skip.", listInSchema: false);

      this.DirectiveHandlerTypes.AddRange( new Type[] {
         typeof(DeprecatedDirectiveHandler), typeof(IncludeDirectiveHandler), typeof(SkipDirectiveHandler)
      });
    }
  }
}
