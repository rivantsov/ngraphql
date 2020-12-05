using NGraphQL.CodeFirst;
using NGraphQL.Core.Scalars;

namespace NGraphQL.Core {

  /// <summary>Core module defines standard and custom scalars; @include and @skip directives.</summary>
  public class CoreModule : GraphQLModule {
    public StringScalar String_;
    public IntScalar Int_;
    public LongScalar Long_;
    public FloatScalar Float_;
    public DoubleScalar Double_;
    public BooleanScalar Boolean_;
    public DateTimeScalar DateTime_;
    public DateScalar Date_;
    public TimeScalar Time_;
    public UuidScalar Uuid_;
    public DecimalScalar Decimal_; 

    public IdScalar Id_;

    public CoreModule() {
      // Standard scalar types
      this.String_ = new StringScalar();
      this.Int_ = new IntScalar();
      this.Long_ = new LongScalar();
      this.Float_ = new FloatScalar();
      this.Double_ = new DoubleScalar();
      this.Boolean_ = new BooleanScalar();
      // custom scalars
      this.Id_ = new IdScalar();
      this.DateTime_ = new DateTimeScalar();
      this.Date_ = new DateScalar();
      this.Time_ = new TimeScalar();
      this.Uuid_ = new UuidScalar();
      this.Decimal_ = new DecimalScalar();
      
      RegisterScalars(String_, Int_, Long_, Float_, Double_, Boolean_, Id_, 
                         DateTime_, Date_, Time_, Uuid_, Decimal_);

      // Directives 
      RegisterDirectivAttributes(typeof(DeprecatedDirAttribute));
      RegisterResolvers(typeof(IncludeSkipResolvers));
    }
  }
}
