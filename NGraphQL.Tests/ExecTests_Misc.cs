using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;
using NGraphQL.TestApp;
using System.Linq;

namespace NGraphQL.Tests {

  using TDict = Dictionary<string, object>;

  public partial class ExecTests {

    [TestMethod]
    public async Task Test_Misc_Batching() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@"batching (aka Data Loader). Case 1 - simple field on child object (mainOtherThing).
        Notice # of resolver calls - 2; without batching it would be 4 = 1 (things) + 3 (mainOtherThing).
");

      var query = @"
query { 
  things() { 
    id 
    name 
    mainOtherThing      # this field is implemented by resolver method
    {
      name
    }
  }
}";
      var resp = await ExecuteAsync(query);
      var things = resp.Data["things"];
      Assert.IsNotNull(things, "Expected result");
      var thingsList = (IList)things;
      Assert.AreEqual(3, thingsList.Count);
      // check that resp contains certain string deep in object tree
      var otherThing1Name = resp.GetValue<string>("things.#1.mainOtherThing.name");
      Assert.AreEqual("Other-2-a", otherThing1Name, "Missing specific string in the output.");
      // Without batching the resolver call count would be 4 - one for top query 'things()', 
      //  +3 calls for mainOtherThing for each parent Thing.
      // The mainOtherThing(method getMainOtherThing) is using batching; on the first call
      // the resolver covers for all future calls from parent objects; 
      // it posts the values through fieldContext. 
      // So executer calls the getMainOtherThing() resolver only once. Total number of calls is 2. 
      var callCount = TestEnv.LastRequestContext.Metrics.ResolverCallCount;
      Assert.AreEqual(2, callCount, "Expected call count 2.");

      TestEnv.LogTestDescr(@"batching, Case 2 - list field on child object (otherThings). notice # of resolver calls - 2");
      var query2 = @"
query { 
  things() { 
    id 
    name 
    otherThings {
      name
    }
  }
}";
      var resp2 = await ExecuteAsync(query2);
      var tn = resp2.GetValue<string>("things.#0.name");
      Assert.IsNotNull(tn, "Expected result");
      // check that resp contains certain string deep in object tree
      var otherThingName = resp2.GetValue<string>("things.#0.otherThings.#1.name");
      Assert.AreEqual("Other-1-b", otherThingName, "Missing specific string in the output.");
      callCount = TestEnv.LastRequestContext.Metrics.ResolverCallCount;
      Assert.AreEqual(2, callCount, "Expected call count 2.");
    }

    [TestMethod]
    public async Task Test_Misc_Exceptions () {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr(@"handling 'unexpected' exceptions in resolver methods.");
      // exception in resolver
      query = @"
# exception in resolver and in field reader
query { 
  # Three queries will be executed in parallel. All with throw, expect 3 exc
  th1: things { 
    id 
    name 
    otherThings {
      getNameOrThrowAsync  # async resolver method
    }
  }
  th2: things { 
    id 
    name 
    otherThings {
      getNameOrThrow  # sync resolver method
    }
  }
  th3: things { 
    id 
    name 
    otherThings {
      nameOrThrow      # this is plain property, it throws on some objects
    }
  }

}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(3, resp.Errors.Count, "Expected 2 error(s)");
      var expected = new List<string>() { 
             "Exception thrown by NameOrThrow.", 
            "Exception thrown by GetNameOrThrowAsync.",
            "Exception thrown by GetNameOrThrow."
      };
      Assert.IsTrue(expected.Contains(resp.Errors[0].Message), "error message missing.");
      Assert.IsTrue(expected.Contains(resp.Errors[1].Message), "error message missing.");
      Assert.IsTrue(expected.Contains(resp.Errors[2].Message), "error message missing.");
    }

    [TestMethod]
    public async Task Test_Misc_Mutations() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@"mutations.");
      var mutReq = @"
mutation myMut { 
  mutateThing(id:1, newName: ""NewName1"") { 
    id 
    name 
  }
}";
      var resp = await ExecuteAsync(mutReq);
      var newName = resp.GetValue<string>("mutateThing.name");
      Assert.AreEqual("NewName1", newName, "new name mismatch");
      var th1 = ThingsApp.Instance.Things.First(t => t.Id == 1);
      Assert.AreEqual("NewName1", th1.Name, "new name mismatch");
      // undo the change
      th1.Name = "Name1";
    }

    [TestMethod]
    public async Task Test_Misc_Unions() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr(@" handling Union return type.");
      query = @"
query { 
  #  return type is a list of Union of ApiThing|ApiOtherThing
  uList: getThingsUnionList() { __typename name }
}";
      resp = await ExecuteAsync(query);
      var unionList = resp.GetValue<IList>("uList");
      Assert.AreEqual(2, unionList.Count, "Expected 2 objects in union list");
    }

    [TestMethod]
    public async Task Test_Misc_Interfaces() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr(@" handling Interface return types.");
      query = @"
query { 
        # return type is [NamedObj], NamedObj is interface
  list: getSomeNamedObjects() { __typename, name }
}";
      resp = await ExecuteAsync(query);
      var resList = resp.GetValue<IList>("list");
      Assert.AreEqual(2, resList.Count, "Expected 2 objects");
    }



    [TestMethod]
    public async Task Test_Misc_IncludeSkip() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;
      TDict vars;


      TestEnv.LogTestDescr(@" @include directive with arg value from variable.");
      query = @"
query myQuery ($all: Boolean!) { 
  thing: getThing(id:1) { 
    id 
    name 
    kind @include(if: $all)
  }
}";
      vars = new TDict() { { "all", true } };
      resp = await ExecuteAsync(query, vars);
      var thingScope = resp.GetValue<ObjectScope>("thing");
      var hasKind = thingScope.ContainsKey("kind");
      Assert.IsTrue(hasKind, "Expected kind field.");

      TestEnv.LogTestDescr(@" same, with $all=false.");
      // Now $all=false
      vars["all"] = false;
      resp = await ExecuteAsync(query, vars);
      thingScope = resp.GetValue<ObjectScope>("thing");
      hasKind = thingScope.ContainsKey("kind");
      Assert.IsFalse(hasKind, "Expected no kind field.");
    }

    [TestMethod]
    public void Test_Misc_AsyncMethods() {
      TestEnv.LogTestMethodStart();
      TestEnv.LogTestDescr(@" async calls, verifying that stack unwinds when resolver code awaits.");
      // We will fire async request, we call it synchonously without await, and it will return Task. 
      // The target method awaits a static shared int to become positive; 
      // once we get incomplete task from the method, we set the value to current seconds, wait for task to complete,
      // and check the returned value. 
      // We verify that async method unwinds the call stack all the way when it awaits for a slow event. 
      var req = new GraphQLRequest() {
        Query = @" query { v: waitForPositiveValueAsync }"
      };
      // The query returns whatever is in the ThingsBizApp.WaitValue static field, but it waits until value is > 0
      ThingsApp.WaitValue = -1;
      var task = TestEnv.ThingsServer.ExecuteAsync(req);
      Assert.IsFalse(task.IsCompleted, "expected pending task");
      // Once we set the field, the task will be completed and result returned. 
      var testValue = ThingsApp.WaitValue = AppTime.Now.TimeOfDay.Seconds;
      task.Wait();
      var retValue = (int)task.Result.Data["v"];
      Assert.AreEqual(testValue, retValue, "Returned value does not match test value.");
    }

    [TestMethod]
    public async Task Test_RequestTiming() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      query = @" 
query myQuery { 
  # these two operations will be executed in parallel
   th1: things { 
         name, 
         nextThing { name }, 
         kind, theFlags  
   }  
   th2: things {
         id,
         name, 
         nextThing { name }, 
         kind, theFlags  
   }  
}";

      TestEnv.LogTestDescr("Run 1: warm-up, query not in cache, resolver method might need JITed, so the call might be relatively slow, 10 ms or more");
      resp = await ExecuteAsync(query);
      var th1 = resp.GetValue<IList>("th1");
      Assert.AreEqual(3, th1.Count, "Invalid thing count");

      TestEnv.LogTestDescr("Run 2, same query with disabled query cache, so query should be parsed again, but now with path warmed up - should be 1 ms or less.");
      var reqCache = TestEnv.ThingsServer.RequestCache;
      try {
        reqCache.Enabled = false;
        resp = await ExecuteAsync(query);
      } finally {
        reqCache.Enabled = true;
      }
      th1 = resp.GetValue<IList>("th1");
      Assert.AreEqual(3, th1.Count, "Invalid thing count");

      TestEnv.LogTestDescr("Run 3: all warmed up, parsed query is retrieved from cache, not parsed. Should be really fast.");
      resp = await ExecuteAsync(query);
      th1 = resp.GetValue<IList>("th1");
      Assert.AreEqual(3, th1.Count, "Invalid thing count");
    }


  } //class
}
