using System;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Scalars;

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
      this.DirectiveAttributeTypes.AddRange(new Type[] { 
        typeof(DeprecatedDirAttribute), typeof(IncludeDirAttribute), typeof(SkipDirAttribute)
      });
      this.DirectiveHandlerTypes.AddRange( new Type[] {
         typeof(DeprecatedDirectiveHandler), typeof(IncludeDirectiveHandler), typeof(SkipDirectiveHandler)
      });
    }
  }
}
