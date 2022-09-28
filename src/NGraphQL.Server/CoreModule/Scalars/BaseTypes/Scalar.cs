using System;
using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Server;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core.Scalars {

  public class Scalar {
    public GraphQLApiModel Model { get; private set; } //assigned at CompleteInit
    public readonly string Name;
    public readonly string Description;
    public Type DefaultClrType;
    public readonly bool IsCustom;
    public bool IsDefaultForClrType { get; protected set; } = true;
    public Type[] CanConvertFrom;
    public bool CanHaveSelectionSubset { get; protected set; }

    public const string DefaultSpecifiedBy = "https://github.com/rivantsov/ngraphql/wiki";
    public string SpecifiedByUrl = DefaultSpecifiedBy;


    public Scalar(string name, string description, Type defaultClrType, bool isCustom = true) {
      Name = name;
      Description = description;
      DefaultClrType = defaultClrType;
      IsCustom = isCustom;
    }

    public virtual object ParseValue(RequestContext context, ValueSource valueSource) {
      if(valueSource is TokenValueSource tknVsrc)
        return ParseToken(context, tknVsrc.TokenData);
      throw new InvalidInputException("invalid input value, expected scalar", valueSource);
    }

    public virtual object ParseToken(RequestContext context, TokenData tokenData) {
      return tokenData.ParsedValue; // this is value converted by Irony parser
    }

    public virtual object ConvertInputValue(RequestContext context, object inpValue) {
      return inpValue;
    }

    // used in Schema doc output - to format constants in default values of args
    public virtual string ToSchemaDocString(object value) {
      if (value == null)
        return "null";
      return value.ToString();
    }

    public virtual object ToOutput(IFieldContext context, object value) {
      return value;
    }

    public virtual void CompleteInit(GraphQLApiModel model) {
      Model = model;  
    }

  }
}
