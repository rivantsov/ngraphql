using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Utilities;

namespace NGraphQL.Model.Construction {

  public partial class ModelBuilder {

    private void ProcessResolverClasses() {
      // We create Query type upfront; we create Mutation type when we find first mutation
      //  (mutations are optional in GraphQL model, query is required)
     // _model.QueryType = new ObjectTypeDef("Query", null) { };

      var resolverClasses = _api.Modules.SelectMany(m => m.ResolverClasses).ToList();
      foreach(var resClass in  resolverClasses) {
        var methods = resClass.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        foreach(var method in methods) {
          var resAttr = method.GetCustomAttribute<ResolverTargetBaseAttribute>();
          if(resAttr == null)
            continue; // not a resolver method
          var fld = CreateFieldWithResolver(resClass, method, resAttr);
          if (fld == null)
            continue; // it is an error, error is already recorded
          // if the resolver method has [OnType] attribute on 1st parameter, we place the field on the target type
          if (fld.Resolver.OnType != null) {
            AddOrReplaceFieldDefOnTargetType(fld); 
            continue;
          }
          // regular operations on root Query and Mutation types
          switch (fld.Resolver.OperationType) {
            case OperationType.Query:
              _model.QueryType.Fields.Add(fld);
              break;

            case OperationType.Mutation:
              if(_model.MutationType == null) // create it if needed - Mutation root type is optional
                _model.MutationType = new ObjectTypeDef("Mutation", null);
              _model.MutationType.Fields.Add(fld);
              break;

            case OperationType.Subscription:
              if(_model.SubscriptionType == null) // create it if needed - Mutation root type is optional
                _model.SubscriptionType = new ObjectTypeDef("Subscription", null);
              _model.SubscriptionType.Fields.Add(fld);
              break;

          } //switch
        } // foreach method
      } // foreach resClass

      // add these 2 at the end, so that they appear at the end of the listing
      _model.Types.Add(_model.QueryType);
      if (_model.MutationType != null)
        _model.Types.Add(_model.MutationType);
      if(_model.SubscriptionType != null)
        _model.Types.Add(_model.SubscriptionType);
    }

    private void AddOrReplaceFieldDefOnTargetType(FieldDef field) {
      var onType = field.Resolver.OnType; 
      var fldsContainer = onType as ComplexTypeDef;
      if (fldsContainer == null) {
        AddError($"Invalid target type '{onType}' in [OnType] attribute, must be object type.");
        return; 
      }
      // find existing field explicitly defined - it can be property only (field without args)
      var fields = fldsContainer.Fields;
      var oldField = fields.FirstOrDefault(f => f.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
      if (oldField != null) {
        // replace it
        var index = fields.IndexOf(oldField);
        fields[index] = field; 
      } else 
        fldsContainer.Fields.Add(field); 
    }

    // returns null if errors detected
    private FieldDef CreateFieldWithResolver(ResolverClassInfo classInfo, MethodInfo resolverMethod, ResolverTargetBaseAttribute resAttr) {
      var fieldName = GetFieldNameFromResolverMethod(resolverMethod, resAttr);
      // return type      
      if(resolverMethod.ReturnType == typeof(void)) {
        AddError($"Resolver method '{resolverMethod.Name}' returns void - this is not allowed; resolvers must return real values.");
        return null; 
      } 
      // Check if it is async method returning task
      var retType = resolverMethod.ReturnType;
      var returnsTask = retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(Task<>);
      Func<object, object> taskResultReader = null; 
      if(returnsTask) {
        retType = retType.GetGenericArguments()[0];
        taskResultReader = ReflectionHelper.CompileTaskResultReader(retType);
      }
      var returnTypeRef = GetTypeRef(retType, resolverMethod, $"Method {resolverMethod.Name}");

      // OnType - check if this resolver is for field on a type, not root query
      TypeDefBase onTypeDef = null;
      var onType = (resAttr as FieldAttribute)?.OnType;
      if (onType != null) {
        onTypeDef = _model.LookupTypeDef(onType);
        if (onTypeDef == null) {
          AddError($"Resover method '{resolverMethod.Name}': OnType '{onType.Name}' not registered.");
          return null; 
        }
      }

      // create field
      var resInfo = new ResolverMethodInfo() { ClassInfo = classInfo, OnType = onTypeDef, 
        ReturnsTask = returnsTask, TaskResultReader = taskResultReader,
        OperationType = resAttr.OperationType, Method = resolverMethod, Attribute = resAttr, FieldName = fieldName
      };
      var dirs = BuildDirectivesFromAttributes(resolverMethod);
      var fldDef = new FieldDef(resInfo.FieldName, returnTypeRef) {  Resolver = resInfo, Directives = dirs };
      if(resolverMethod.HasAttribute<HiddenAttribute>())
        fldDef.Flags |= FieldFlags.Hidden;
      if(returnsTask)
        fldDef.Flags |= FieldFlags.ReturnsTask;
      fldDef.Description = _docLoader.GetDocString(resolverMethod, resolverMethod.DeclaringType);

      // Analyze method parameters and build field args
      var prms = resolverMethod.GetParameters();
      if (prms.Length == 0 || prms[0].ParameterType != typeof(IFieldContext)) {
        AddError($"Resolver method {resolverMethod.Name}: the first parameter must be of type '{nameof(IFieldContext)}'.");
        return fldDef;
      }

      // validate parameters
      for(int i = 1; i < prms.Length; i++) { //starting with 1, FieldContext already checked
        var prm = prms[i];
        if(i == 1 && onType != null) {
          // it is auto param, parent object; prm.Type is entity type, check it is mapped to OnType  
          var mappedTo = _model.GetMappedGraphQLType(prm.ParameterType);
          if (mappedTo != onTypeDef) {
            AddError($"Resolver method {resolverMethod.Name}: invalid parameter {prm.Name}, expected entity type mapped to OnType type '{onType.Name}'.");
            continue; 
          }
          fldDef.Flags |= FieldFlags.HasParentArg;
          continue; 
        }
        var prmTypeRef = GetTypeRef(prm.ParameterType, prm, $"Method {resolverMethod.Name}, parameter {prm.Name}");
        if (prmTypeRef.IsList && !prmTypeRef.TypeDef.IsEnumFlagArray())
          VerifyListParameterType(prm.ParameterType, resolverMethod.Name, prm.Name); 
        var prmDirs = BuildDirectivesFromAttributes(prm);
        var dftValue = prm.DefaultValue == DBNull.Value ? null : prm.DefaultValue;
        var argDef = new InputValueDef() { Name = GetGraphQLName(prm), TypeRef = prmTypeRef, 
                        ParamType = prm.ParameterType, HasDefaultValue = prm.HasDefaultValue, 
                        DefaultValue = dftValue, Directives = prmDirs };
        fldDef.Args.Add(argDef); 
      }
      return fldDef; 
    }

    private void VerifyListParameterType(Type type, string methodName, string paramName) {
      if (!type.IsArray && !type.IsInterface) 
        AddError($"Invalid list parameter type - must be array or IList<T>; resolver {methodName}, parameter {paramName}. ");
    }

    private string GetFieldNameFromResolverMethod(MethodInfo resolverMethod, ResolverTargetBaseAttribute resolverAttr) {
      var name = resolverAttr.FieldName; 
      if (string.IsNullOrWhiteSpace(name)) {
        name = GetGraphQLName(resolverMethod);
        /* cutting off 'get' prefix - decided not to do this, more confusion than benefits
        if(resolverAttr.OperationType == OperationType.Query && name.Length > 3 && name.StartsWith("get"))
          name = name.Substring(3).FirstLower();  // cut off Get prefix
          */
      }
      return name; 
    }

  }
}
