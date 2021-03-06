﻿using System;
using NGraphQL.CodeFirst;
using NGraphQL.Model.Request;
using NGraphQL.Server.Execution;

namespace NGraphQL.Core.Scalars {

  public class Scalar {
    public readonly string Name;
    public readonly string Description;
    public Type DefaultClrType;
    public readonly bool IsCustom;
    public bool IsDefaultForClrType { get; protected set; } = true;
    public Type[] CanConvertFrom;

    public Scalar(string name, string description, Type defaultClrType, bool isCustom = true) {
      Name = name;
      Description = description;
      DefaultClrType = defaultClrType;
      IsCustom = isCustom;
    }

    public virtual object ParseToken(RequestContext context, TokenData tokenData) {
      return tokenData.ParsedValue; // this is value converted by Irony parser
    }

    public virtual object ConvertInputValue(RequestContext context, object inpValue) {
      return inpValue;
    }

    // used in Schema doc output
    public virtual string ToSchemaDocString(object value) {
      if (value == null)
        return "null";
      return value.ToString();
    }

    public virtual object ToOutput(IFieldContext context, object value) {
      return value;
    }

  }
}
