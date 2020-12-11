using System.Collections.Generic;
using System.Linq;
using System.Text;
using NGraphQL.Core;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Server.RequestModel;

namespace NGraphQL.Model {

  public class SchemaDocGenerator {
    GraphQLApiModel _model;
    StringBuilder _builder;
    public string Indent = "  ";

    public SchemaDocGenerator() {
    }

    public string GenerateSchema(GraphQLApiModel model) {
      _model = model;
      _builder = new StringBuilder();

      // custom scalar types 
      var scalarTypes = SelectTypes<ScalarTypeDef>(TypeKind.Scalar)
          .Where(td => td.Scalar.IsCustom).ToList(); 
      foreach(var sd in scalarTypes) {
        AppendDescr(sd.Description);
        _builder.AppendLine("scalar " + sd.Name);
      }
      _builder.AppendLine();

      // enums 
      var enumDefs = SelectTypes<EnumTypeDef>(TypeKind.Enum); 
      foreach(var enumDef in enumDefs) {
        AppendDescr(enumDef.Description);
        _builder.AppendLine("enum " + enumDef.Name + " {");
        foreach(var m in enumDef.EnumValues) {
          AppendDescr(m.Description, true);
          _builder.AppendLine(Indent + m.Name);
        }
        _builder.AppendLine("}");
        _builder.AppendLine();
      }

      // interfaces
      var intfTypes = SelectTypes<InterfaceTypeDef>(TypeKind.Interface);
      foreach(var tDef in intfTypes) {
        AppendDescr(tDef.Description);
        _builder.AppendLine("interface " + tDef.Name + " {");
        foreach(var fld in tDef.Fields) {
          if(fld.Flags.IsSet(FieldFlags.Hidden))
            continue;
          Append(fld);
          _builder.AppendLine(); 
        }
        _builder.AppendLine("}");
        _builder.AppendLine();
      }

      // Input types
      var inpTypes = SelectTypes<InputObjectTypeDef>(TypeKind.InputObject);
      foreach(var tDef in inpTypes) {
        AppendDescr(tDef.Description);
        _builder.Append("input " + tDef.Name);
        _builder.AppendLine(" {");
        foreach(var fldDef in tDef.Fields) {
          Append(fldDef, indent: true);
          _builder.AppendLine();
        }
        _builder.AppendLine("}");
        _builder.AppendLine();
      }

      // Unions
      var unionTypes = SelectTypes<UnionTypeDef>(TypeKind.Union);
      foreach(var tDef in unionTypes) {
        var typeNames = string.Join(" | ", tDef.PossibleTypes.Select(t => t.Name));
        AppendDescr(tDef.Description);
        _builder.AppendLine($"union {tDef.Name} = {typeNames}");
        _builder.AppendLine();
      }

      // types
      var objTypes = SelectTypes<ObjectTypeDef>(TypeKind.Object);
      foreach(var tDef in objTypes) {
        AppendDescr(tDef.Description);
        _builder.Append("type " + tDef.Name);
        if(tDef.Implements.Count > 0) {
          _builder.Append(" implements ");
          var intfList = string.Join(" & ", tDef.Implements.Select(iDef => iDef.Name));
          _builder.Append(intfList);
        }
        _builder.AppendLine(" {");
        foreach(var fld in tDef.Fields) {
          if(fld.Flags.IsSet(FieldFlags.Hidden))
            continue;
          Append(fld);
          _builder.AppendLine();
        }
        _builder.AppendLine("}");
        _builder.AppendLine();
      }

      return _builder.ToString(); 
    }

    private void AppendDirs(IList<HandlesDirectiveAttribute> dirs) {
      if(dirs == null || dirs.Count == 0)
        return; 
      foreach(var dir in dirs) {
        _builder.Append(" ");
        _builder.Append(dir.Name);
        if (dir.Def.Args.Count > 0) {
          var nvList = new List<string>(); 
          for(int i = 0; i < dir.Def.Args.Count; i++) {
            if (dir.ArgValues[i] == null)
              continue; //just skip it
            var strArg = FormatArg(dir.Def.Args[i], dir.ArgValues[i]);
            nvList.Add(strArg); 
          }
          if (nvList.Count > 0) {
            _builder.Append("(");
            var strArgs = string.Join(", ", nvList);
            _builder.Append(strArgs);
            _builder.Append(")");
          }
        } //if
      }
    } //method

    private string FormatArg(InputValueDef argDef, object value) {
      string strV;
      if(value == null)
        strV = "null";
      else
        strV = argDef.TypeRef.TypeDef.ToSchemaDocString(value);
      return $"{argDef.Name}: {strV}";
    }

    private void Append(FieldDef field) {
      AppendDescr(field.Description, true);
      _builder.Append(Indent); 
      _builder.Append(field.Name);
      if(field.Args.Count > 0) {
        var argsToPrint = field.Args;
        if(argsToPrint.Count > 0) {
          _builder.Append(" (");
          for(int i = 0; i < argsToPrint.Count; i++) {
            var arg = argsToPrint[i];
            if(i > 0)
              _builder.Append(", ");
            Append(arg); 
          }
          _builder.Append(")");
        } //if argsToPrint
      } //if args.Coun > 0
      _builder.Append(": ");
      _builder.Append(field.TypeRef.Name);
    }

    private void Append(InputValueDef valueDef, bool indent = false) {
      AppendDescr(valueDef.Description, true);
      if(indent)
        _builder.Append(Indent);
      _builder.Append(valueDef.Name);
      _builder.Append(": ");
      _builder.Append(valueDef.TypeRef.Name);
      if(valueDef.HasDefaultValue) {
        _builder.Append(" = ");
        var tdef = valueDef.TypeRef.TypeDef; 
        _builder.Append(tdef.ToSchemaDocString(valueDef.DefaultValue));
      }
      AppendDirs(valueDef.Directives);
    }

    private void AppendDescr(string descr, bool indent = false) {
      const string Q3 = "\"\"\"";
      if(string.IsNullOrWhiteSpace(descr))
        return;
      // make an extra empty line before description string
      _builder.AppendLine(); 
      if (descr.Contains("\n")) {
        _builder.AppendLine(Q3);
        _builder.AppendLine(descr);
        _builder.AppendLine(Q3);
      } else {
        descr = Escape(descr);
        if(indent)
          _builder.Append(Indent);
        _builder.AppendLine("\"" + descr + "\"");
      }
    }

    private IList<T> SelectTypes<T>(TypeKind kind) where T: TypeDefBase {
      return _model.GetTypeDefs<T>(kind, excludeHidden: true);
    }

    static char[] _toEscape = new char[] { '\\', '"' };

    private string Escape(string str) {
      if(str.IndexOfAny(_toEscape) < 0)
        return str;
      str = str.Replace("\\", "\\\\").Replace("\"", "\\\"");
      return str; 
    }

  }//class
}
