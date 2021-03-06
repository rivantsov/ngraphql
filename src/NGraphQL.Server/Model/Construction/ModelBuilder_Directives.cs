﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NGraphQL.CodeFirst;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  partial class ModelBuilder {


    private bool BuildRegisteredDirectiveDefinitions() {
      RegisterAllModuleDirectives();
      // in a separate loop, after all dirs are registered, map directive handlers
      MapModuleDirectiveHandlers();
      return !_model.HasErrors;
    }

    private void RegisterAllModuleDirectives() {
      foreach (var module in _server.Modules) {
        foreach (var dirReg in module.Directives) {
          var dirName = dirReg.Name.TrimStart('@');
          if (_model.Directives.ContainsKey(dirName)) {
            AddError($"Module {module.Name}: directive @{dirName} already registered.");
            continue;
          }
          if (dirReg.Signature == null && dirReg.AttributeType == null) {
            AddError($"Module {module.Name}: directive @{dirName} has no Signature method or Attribute type.");
            continue; 
          }
          ICustomAttributeProvider attrSrc;
          if (dirReg.AttributeType != null) {
            attrSrc = dirReg.AttributeType;
            var constrList = dirReg.AttributeType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constrList.Length != 1) {
              AddError($"Module {module.Name}: directive @{dirName}, the attribute {dirReg.AttributeType} " +
                " must have a single public constructor as a template for directive signature.");
              continue;
            }
            dirReg.Signature = constrList[0];
          } else {
            attrSrc = dirReg.Signature;
          }
          //  create dir def
          // first check secondary depr attribute - in case the target directive is deprecated itself
          var deprAttrSec = attrSrc.GetAttribute<DeprecatedDirAttribute>();
          var prms = dirReg.Signature.GetParameters();
          var argDefs = BuildArgDefs(prms, dirReg.Signature);
          var dirDef = new DirectiveDef() {
            Registration = dirReg, Name = dirName, Description = dirReg.Description, DeprecatedAttribute = deprAttrSec,
            Args = argDefs
          };
          _model.Directives[dirName] = dirDef;
        }
      } //foreach module
    }

    private void MapModuleDirectiveHandlers() {
      foreach (var module in _server.Modules) {
        foreach (var dirInfo in module.DirectiveHandlers) {
          var dirName = dirInfo.Name;
          if (!_model.Directives.TryGetValue(dirName, out var dirDef)) {
            AddError($"Module {module.Name}: directive handler targets directive {dirName} which is not registered.");
            continue;
          }
          if (dirDef.Handler != null) {
            AddError($"Module {module.Name}: handler type for directive {dirName} is already registered.");
            continue; 
          }
          if (dirInfo.Type == null) {
            AddError($"Module {module.Name}: handler type for directive {dirName} may not be null.");
            continue;
          }
          dirDef.Handler = Activator.CreateInstance(dirInfo.Type) as IDirectiveHandler;
          if (dirDef.Handler == null) {
            AddError($"Module {module.Name}: handler type for directive {dirName} is invalid, does not implement {nameof(IDirectiveHandler)} interface.");
          }
        }
      } //foreach module
      if (_model.HasErrors)
        return; 
      // now check that all directives have handlers
      foreach(var dirDef in _model.Directives.Values) {
        if (dirDef.Handler == null)
          AddError($"Directive {dirDef.Name} has no directive handler.");
      }
    }

    private IList<ModelDirective> BuildDirectivesFromAttributes(ICustomAttributeProvider clrObjectInfo, DirectiveLocation location) {
      var attrList = clrObjectInfo.GetCustomAttributes(inherit: true);
      if (attrList.Length == 0)
        return ModelDirective.EmptyList;

      var dirList = new List<ModelDirective>();
      foreach (var attr in attrList) {
        if (!(attr is BaseDirectiveAttribute dirAttr))
          continue;
        var dirAttrType = dirAttr.GetType(); 
        var dirDef = _model.Directives.Values.FirstOrDefault(def => def.Registration.AttributeType == dirAttrType);
        if (dirDef == null) {
          AddError($"{clrObjectInfo}: directive attribute {dirAttrType} not registered.");
          continue;
        }
        var dir = new ModelDirective() { Def = dirDef, ModelAttribute = dirAttr, Location = location, ArgValues = dirAttr.ArgValues }; 
        dirList.Add(dir);
      }
      return dirList;
    } //method

  } //class
}
