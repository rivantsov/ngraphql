using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Utilities;

namespace NGraphQL.Tests {
  using TDict = Dictionary<string, object>;

  public partial class ExecTests {

    [TestMethod]
    public async Task Test_Fragments_Errors() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr("error - invalid (unknown) field in a fragment ");
      query = @"
query myQuery { 
  getThing(id: 1) {
    ...Fr1 
  }
}
fragment Fr1 on Thing {
    nameX
    ...Fr2
}
fragment Fr2 on NamedObj {
    name2
}
";
      // errors: 
      //   Fragment Fr1: field nameX not defined on type Thing.
      //   Fragment Fr2: field name2 not defined on type NamedObj.
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(2, resp.Errors.Count, "Expected 2 errors.");

      TestEnv.LogTestDescr("error - self-ref and circular ref fragments; circular refs are invalid at top level.");
      query = @"
query myQuery { 
  getThing(id: 1) {
    ...Fr1 
  }
}
fragment Fr1 on Thing {
    name
    ...Fr2
}
fragment Fr2 on Thing {
    name
    ...Fr1
}
";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(2, resp.Errors.Count, "Expected 2 errors.");
      var isSelfRef = resp.Errors.All(err => err.Message.Contains("self-referencing"));
      Assert.IsTrue(isSelfRef, "Expected 'fragment self-referencing' errors only.");

      TestEnv.LogTestDescr("self-referencing fragment; self-ref and circular refs are OK when inside selection subsets.");
      query = @"
query myQuery { 
  getThing(id: 1) {
    ...ThingDetails 
  }
}
fragment ThingDetails on Thing {
    name
    nextThing {
      ...ThingDetails
    }
}
";
//      Assert.IsTrue(false, "Fix this: self-ref fragment crashes the test process.");
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(0, resp.Errors.Count, "Expected no errors.");

      TestEnv.LogTestDescr("using self-referencing fragment to unfold the type definition");
      query = @"
#                      querying list of fields of an input type; print out field name, type
query {
  __type (name: ""InputObjWithList"") {
    name
    kind
    inputFields {
      name
      type {
        ...TypeDetails
      }
    }
  }
}
fragment TypeDetails on __Type {
    displayName      # this is our extension of the spec
    kind
    ofType {
      ...TypeDetails
    }
}
";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(0, resp.Errors.Count, "Expected no errors.");
    }


    [TestMethod]
    public async Task Test_Fragments() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;
      TDict vars;

      // --------------------------------------------------------------------------------------------
      TestEnv.LogTestDescr("fragment with object field and selection subset.");
      query = @"
query myQuery { 
  getThing(id: 1) {
    ...Flds 
  }
}
fragment Flds on Thing {
    name
    nextThing {
      name
      nextId: id
    }
}
";
      resp = await ExecuteAsync(query);
      // check that nextId appears in the output
      var nextId = resp.Data.GetValue<int>("getThing/nextThing/nextId");
      Assert.IsTrue(nextId > 0, "next id expected > 0");

      // --------------------------------------------------------------------------------------------
      TestEnv.LogTestDescr("inline fragment with 'On' condition.");
      query = @"
query myQuery { 
  getThing(id: 1) {
    name
    ... on Thing {
      id
    }
  }
}
";
      resp = await ExecuteAsync(query);
      // check that nextId appears in the output
      var id = resp.Data.GetValue<int>("getThing/id");
      Assert.IsTrue(nextId > 0, "id expected > 0");

      // --------------------------------------------------------------------------------------------
      TestEnv.LogTestDescr("inline fragment without 'On' condition and with @include(if:true).");
      query = @"
query myQuery ($inc: Boolean) { 
  getThing(id: 1) {
    name
    ... @include(if: $inc) {
      id
    }
  }
}
";
      vars = new TDict() { { "inc", true } };
      resp = await ExecuteAsync(query, vars);
      // check that id appears in the output
      id = resp.Data.GetValue<int>("getThing/id");
      Assert.IsTrue(id > 0, "id expected > 0");

      TestEnv.LogTestDescr("same but with @include(if:false)");
      vars = new TDict() { { "inc", false } };
      resp = await ExecuteAsync(query, vars);
      // check that id does not appear in the output; GetValue should return 0 (default int)
      id = resp.Data.GetValue<int>("getThing/id");
      Assert.AreEqual(0, id, "id expected == 0");


      TestEnv.LogTestDescr("query on union with fragment with conditional-on-type inline fragments ");
      query = @"
query { 
  uList: getThingsUnionList() { 
    ...UnionFields  
  }
}
fragment UnionFields on ThingsUnion {
    __typename
    name
    ... on Thing {
      nextThing {
        nextName: name
      }
    }
    ... on OtherThing {
      idStr
    }
}";
      resp = await ExecuteAsync(query);
      var nextName = resp.GetValue<string>("uList/#0/nextThing/nextName");
      Assert.IsNotNull(nextName, "Expected nextName");
      var idStr = resp.GetValue<string>("uList/#1/idStr");
      Assert.IsNotNull(idStr, "Expected idStr");
    }



    [TestMethod]
    public async Task Test_FragmentsInline() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;
      TestEnv.LogTestDescr("Testing inline fragment");

      query = @"
query  { 
  getSomeNamedObjects {
    name
    ... on OtherThing {
      idStr
    }
  }
}
";
      // we just testing it won't fail on idStr
      resp = await ExecuteAsync(query);
      var list = resp.Data.GetValue<IList>("getSomeNamedObjects");
      Assert.IsTrue(list.Count > 0);
    }
  } //class
}
