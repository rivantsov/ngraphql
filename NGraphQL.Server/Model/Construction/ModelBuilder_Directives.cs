using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using NGraphQL.CodeFirst;
using NGraphQL.Core;
using NGraphQL.Directives;
using NGraphQL.Introspection;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {
  partial class ModelBuilder {

    private bool BuildRegisteredDirectiveDefinitions() {
      foreach (var module in _server.Modules)
        RegisterModuleDirectives(module);
      // in a separate loop, after all dirs are registered, map directive handlers
      foreach (var module in _server.Modules)
        MapModuleDirectiveHandlers(module);
      // check that all dirs have handlers
      foreach(var dirDef in _model.Directives.Values) 
        if (dirDef.DirectiveHandlerType == null)
          AddError($"Directive {dirDef.Name} has no registered directive handler.");
      return !_model.HasErrors;
    }

    private void RegisterModuleDirectives(GraphQLModule module) { 
      foreach (var dirReg in module.RegisteredDirectives) {
        var dirType = dirReg.DirectiveType;
        if (!typeof(IDirectiveInstance).IsAssignableFrom(dirType)) {
          AddError($"Directive attribute {dirType} is not .");
        }
        if (_model.Directives.ContainsKey(dirReg.Name)) {
          AddError($"Module {module.Name}: directive {dirReg.Name}, type {dirType} already registered.");
          continue;
        }
        var dirDef = new DirectiveDef() { DirInfo = dirReg, Name = dirReg.Name, Description = dirReg.Description };
        _model.Directives[dirDef.Name] = dirDef;
      }
    }

    private void MapModuleDirectiveHandlers(GraphQLModule module) {
      foreach (var handlerType in module.DirectiveHandlerTypes) {
        if (!typeof(DirectiveHandler).IsAssignableFrom(handlerType)) {
          AddError($"Module {module.Name}: invalid directive handler {handlerType} - must derive from the {typeof(DirectiveHandler)} type.");
          continue; 
        }
        var handlesAttr = handlerType.GetAttribute<HandlesDirectiveAttribute>();
        if (handlesAttr == null) {
          AddError($"Module {module.Name}: directive handler {handlerType} is missing {typeof(HandlesDirectiveAttribute)}.");
          continue; 
        }
        var dirName = handlesAttr.DirectiveName; 
        if(!_model.Directives.TryGetValue(dirName, out var dirDef)) {
          AddError($"Module {module.Name}: directive handler {handlerType}, target directive {dirName} is not registered.");
          continue;
        }
        dirDef.DirectiveHandlerType = handlerType; 
        // todo: verify handler constructor parameters
      }
    }

    private IList<ModelDirective> BuildDirectivesFromAttributes(ICustomAttributeProvider clrObjectInfo, 
                          DirectiveLocation location, GraphQLModelObject owner) {
      var attrList = clrObjectInfo.GetCustomAttributes(inherit: true);
      if (attrList.Length == 0)
        return ModelDirective.EmptyList;

      var dirList = new List<ModelDirective>();
      foreach (var attr in attrList) {
        if (!(attr is IDirectiveInstance dirAttr))
          continue;
        var dirAttrType = dirAttr.GetType(); 
        var dirDef = _model.Directives.Values.FirstOrDefault(def => def.DirInfo.DirectiveType == dirAttrType);
        if (dirDef == null) {
          AddError($"{clrObjectInfo}: directive attribute {dirAttrType} not registered.");
          continue;
        }
        // create handler instance
        var dirCtx = new DirectiveContext() { Def = dirDef, Location = location, Owner = owner };
        var constrArgs = new object[] { dirCtx, dirAttr.ArgValues };
        var handler = (DirectiveHandler)Activator.CreateInstance(dirDef.DirectiveHandlerType, constrArgs);
        var dir = new ModelDirective() { Def = dirDef, Attribute = dirAttr, Handler = handler, Location = location };
        dirList.Add(dir);
      }
      return dirList;
    } //method


  } //class
}
