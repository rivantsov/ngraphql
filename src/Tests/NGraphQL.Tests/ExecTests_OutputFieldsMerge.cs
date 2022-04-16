using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using NGraphQL.Utilities;
using Things;

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

      TestEnv.LogTestDescr(@"Testing merging complex fields");
      query = @"
query { 
  thing: getThing(id: 1) { name kind }
  thing: getThing(id: 1) { tag }
}";
      resp = await ExecuteAsync(query);
      var thing = resp.GetValue<OutputObjectScope>("thing");
      // there should be single 'thing', with combined properties: name, kind, tag
      var name = thing["name"];
      var tag = thing["tag"];
      Assert.IsNotNull(name, "expected thing.Name not null");
      Assert.IsNotNull(tag, "expected thing.Tag not null");


    }

  } //class
}
