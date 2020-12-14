using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {
  partial class ModelBuilder {

    private bool BuildRegisteredDirectiveDefinitions() {
      foreach (var module in _server.Modules)
        RegisterModuleDirectives(module);
      // in a separate loop, after all dirs are registered, map directive handlers
      foreach (var module in _server.Modules)
        MapModuleDirectiveHandlers(module);
      return !_model.HasErrors;
    }

    private void RegisterModuleDirectives(GraphQLModule module) { 
      foreach (var dirType in module.DirectiveAttributeTypes) {
        var infoAttr = dirType.GetAttribute<DirectiveInfoAttribute>();
        if (infoAttr == null) {
          AddError($"Directive attribute {dirType} has no DirectiveInfo attribute.");
          continue;
        }
        var info = infoAttr.Info;
        if (_model.Directives.ContainsKey(info.Name)) {
          AddError($"Module {module.Name}: directive {info.Name}, type {dirType} already registered.");
          continue;
        }
        var dirDef = new DirectiveDef() { AttributeType = dirType, DirInfo = info, Name = info.Name, Description = info.Description };
        _model.Directives[dirDef.Name] = dirDef;
      }
    }

    private void MapModuleDirectiveHandlers(GraphQLModule module) {
      throw new NotImplementedException(); 
    }



    private IList<DirectiveDef> BuildDirectivesFromAttributes(ICustomAttributeProvider target) {
      var attrList = target.GetCustomAttributes(inherit: true);
      if (attrList.Length == 0)
        return DirectiveDef.EmptyList;

      var dirList = new List<DirectiveDef>();
      foreach (var attr in attrList) {
        var attrName = attr.GetType().Name;
        var dirDefType = dirAttr.DirectiveDefType;
        var dirDef = _model.Directives.Values.FirstOrDefault(def => def.GetType() == dirDefType);
        if (dirDef == null) {
          AddError($"{target}: directive definition {dirDefType.Name} referenced by [{attrName}] not registered..");
          continue;
        }

        var dir = dirDef.CreateDirective(_model, dirAttr, target);
        dirList.Add(dir);
      }
      return dirList;
    } //method


  } //class
}
