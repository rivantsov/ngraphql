using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using NGraphQL.CodeFirst;
using NGraphQL.Core.Scalars;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;

namespace NGraphQL.Core {

  /// <summary>Core module defines standard and custom scalars; @include and @skip directives
  /// 
  /// </summary>
  public class CoreModule : GraphQLModule {
    public StringTypeDef String_;
    public IntTypeDef Int_;
    public LongTypeDef Long_;
    public FloatTypeDef Float_;
    public DoubleTypeDef Double_;
    public BooleanScalar Boolean_;
    public DateTimeTypeDef DateTime_;
    public DateTypeDef Date_;
    public TimeTypeDef Time_;
    public UuidTypeDef Uuid_;
    public DecimalTypeDef Decimal_; 

    public IdTypeDef Id_;

    public DeprecatedDirDef DeprecatedDir;
    public IncludeDirectiveDef IncludeDir;
    public SkipDirectiveDef SkipDir; 

    public CoreModule() {
      // Standard scalar types
      this.String_ = new StringTypeDef();
      this.Int_ = new IntTypeDef();
      this.Long_ = new LongTypeDef();
      this.Float_ = new FloatTypeDef();
      this.Double_ = new DoubleTypeDef();
      this.Boolean_ = new BooleanScalar();
      this.Id_ = new IdTypeDef(); 
      // custom scalars
      this.DateTime_ = new DateTimeTypeDef();
      this.Date_ = new DateTypeDef();
      this.Time_ = new TimeTypeDef();
      this.Uuid_ = new UuidTypeDef();
      this.Decimal_ = new DecimalTypeDef();
      
      RegisterScalars(String_, Int_, Long_, Float_, Double_, Boolean_, Id_, 
                         DateTime_, Date_, Time_, Uuid_, Decimal_);

      // Directives 
      DeprecatedDir = new DeprecatedDirDef(this);
      IncludeDir = new IncludeDirectiveDef(this);
      SkipDir = new SkipDirectiveDef(this); 
      RegisterDirectives(DeprecatedDir, IncludeDir, SkipDir);
    }
  }
}
