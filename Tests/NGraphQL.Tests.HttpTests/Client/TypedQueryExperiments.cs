using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using NGraphQL.Client;
using NGraphQL.TestApp;

namespace NGraphQL.Tests.Experiments {

  using static ExpExtensions; 

  public class TypedQueryExperiments {
    
    GraphQLClient _client; 

    public (Thing_, string) GetStuff() {

      var ops = _client.GetOperations<IThingsQuery>();
      var requestExpr = _client.BuildRequest(() => new {
        th1 = ops.GetThing(1).Fields(th => new Thing_() {
          Name = th.Name, 
          NextThing = th.NextThing.Fields(
              nt => new { 
                nt.Id, nt.Name, 
                otherThings = nt.otherThings.ItemFields(ot => new {ot.Name, ot.Strings)}
            ),
          Randoms = th.Randoms,
        }),
        strIntList = ops.EchoIntArray(new int[] { 1, 2, 3}),        
      });
      var result = _client.ExecuteRequest(requestExpr);
      return (result.th1, result.strIntList);

    }

  }

  public class OperationSet<TQuery> {

  }

  public static class ExpExtensions {

    public static TQuery GetOperations<TQuery>(this GraphQLClient client) {
      return default; 
    }

    public static T Fields<T>(this T src, Expression<Func<T, object>> selExpr) {
      return default; 
    }
    public static T ItemFields<T>(this IList<T> src, Expression<Func<T, object>> selExpr) {
      return default;
    }

    public static Expression<TOut> BuildRequest<TOut>(this GraphQLClient client, Expression<Func<TOut>> expr) {
      return default;
    }

    public static TOut ExecuteRequest<TOut>(this GraphQLClient client, Expression<TOut> reqExpr) {
      return default; 
    }

  }
}
