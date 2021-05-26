using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using NGraphQL.CodeFirst;

using NGraphQL.Model;

namespace NGraphQL.Utilities {
  public static class ExpressionHelper {

    public static Func<object, object> CompileResolverExpression(ParameterExpression argExpr, Expression body) {
      var baseLambda = Expression.Lambda(body, argExpr);
      var newParentPrm = Expression.Parameter(typeof(object));
      var parentObj = Expression.Convert(newParentPrm, argExpr.Type);
      var invokeBaseLambdaExpr = Expression.Invoke(baseLambda, parentObj);
      var convResultExpr = Expression.Convert(invokeBaseLambdaExpr, typeof(object));
      var newLambda = Expression.Lambda(convResultExpr, newParentPrm);
      var compiledLambda = newLambda.Compile();
      var func = (Func<object, object>)compiledLambda;
      return func;
    }

    public static Func<object, object> CompileTaskResultReader(Type taskType) {
      var resultProp = taskType.GetProperty("Result");
      var prm = Expression.Parameter(typeof(object));
      var taskExpr = Expression.Convert(prm, taskType);
      var readPropExpr = Expression.MakeMemberAccess(taskExpr, resultProp);
      var resultExpr = Expression.Convert(readPropExpr, typeof(object));
      var lambda = Expression.Lambda(resultExpr, prm);
      var func = (Func<object, object>)lambda.Compile();
      return func;
    }

    public static Func<object, object> CompileMemberReader(MemberInfo member) {
      var prm = Expression.Parameter(typeof(object));
      var objExpr = Expression.Convert(prm, member.DeclaringType);
      var readExpr = Expression.MakeMemberAccess(objExpr, member);
      var resultExpr = Expression.Convert(readExpr, typeof(object));
      var lambda = Expression.Lambda(resultExpr, prm);
      var func = (Func<object, object>)lambda.Compile();
      return func;
    }

  }
}
