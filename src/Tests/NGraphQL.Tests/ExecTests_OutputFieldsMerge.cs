using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;

namespace NGraphQL.Tests {

  partial class ExecTests {

    [TestMethod]
    public async Task Test_OutFieldsMerge() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr(@"Testing merging simple fields (flags enum)");
      query = @"
query { 
  flags: getFlags
  flags: getFlags
}";
      resp = await ExecuteAsync(query); 
      var topScope = (OutputObjectScope) resp.Data;
      Assert.AreEqual(2, topScope.Keys.Count, "expected 2 fields named 'flags'");
      Assert.IsTrue(topScope.Keys.First() == "flags" && topScope.Keys.Last() == "flags", "expected 2 fields named 'flags'");

      TestEnv.LogTestDescr(@"Testing merging complex objects");
      query = @"
query { 
  thing: getThing(id: 1) { id name }
  thing: getThing(id: 1) { tag kind }
}";
      resp = await ExecuteAsync(query);
      // there should be single 'thing', with combined properties: name, kind, tag
      var thing = resp.GetValue<OutputObjectScope>("thing");
      var id = (int)thing["id"];
      var name = (string) thing["name"];
      var tag = (string) thing["tag"];
      var kind = (string)thing["kind"];
      Assert.IsTrue(id > 0, "expected thing.id");
      Assert.IsNotNull(name, "expected thing.name not null");
      Assert.IsNotNull(kind, "expected thing.kind not null");
      Assert.IsNotNull(tag, "expected thing.tag not null");

      // we test that merging process, visiting tree top to bottom, visits
      TestEnv.LogTestDescr(@"Testing merging inside arrays of complex objects");
      query = @"
query { 
  things: getThingsList() {
    next: nextThing { id name  }
    next: nextThing { kind tag}
  }

}";
      resp = await ExecuteAsync(query);
      // dig to things[0].next (it is a Thing), this object must contain all 4 props: id, name, kind, tag
      var things = resp.GetValue<IList<object>>("things");
      Assert.IsTrue(things.Count > 0, "expected list");
      var thing0 = (OutputObjectScope)things[0];
      thing = (OutputObjectScope)thing0["next"];
      id = (int)thing["id"];
      name = (string)thing["name"];
      kind = (string)thing["kind"];
      tag = (string)thing["tag"];
      Assert.IsTrue(id > 0, "expected thing.id");
      Assert.IsNotNull(name, "expected thing.name not null");
      Assert.IsNotNull(kind, "expected thing.kind not null");
      Assert.IsNotNull(tag, "expected thing.tag not null");

      TestEnv.LogTestDescr(@"Testing failed merge");
      query = @"
query { 
  thing: getThing(id: 1) { id name }
  thing: getThing(id: 2) { tag kind }
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.IsTrue(resp.Errors.Count > 0, "Expected failed merge error.");


    }

  } //class
}
