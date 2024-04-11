using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NGraphQL.Tests {

  partial class ExecTests {

    [TestMethod]
    public async Task Test_BugFixes() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;

      TestEnv.LogTestDescr(@"big decimal input values ");
      decimal decInp = 12345678901234567890m;
      query = @"
query { 
  dv: decTimesTwo(dec: decInp)
}".Replace("decInp", decInp.ToString());
      resp = await ExecuteAsync(query);
      var res = (decimal) resp.Data["dv"];
      Assert.AreEqual(decInp * 2, res, "dec * 2 does not match.");


      TestEnv.LogTestDescr(@"big decimal input values, now with fractions ");
      decInp = 12345678901234567890.987m;
      query = @"
query { 
  dv: decTimesTwo(dec: decInp)
}".Replace("decInp", decInp.ToString());
      resp = await ExecuteAsync(query);
      res = (decimal)resp.Data["dv"];
      Assert.AreEqual(decInp * 2, res, "dec * 2 does not match.");

      TestEnv.LogTestDescr(@"Repro issue #169 in VITA.");
      // reported in VITA: https://github.com/rivantsov/vita/issues/169

      query = @"
query { 
  things() { 
    id 
    name 
    otherThingWrapped {
      otherThingName
      otherThing { name}
    }
    # testing fix for interface entities mapping
    intfThing { id name tag} 
  }
}";
      resp = await ExecuteAsync(query);
      var things = resp.Data["things"];
      Assert.IsNotNull(things, "Expected result");

      // bug, issue #8; resolver returning null on non-null field should cause error
      //   getInvalidthing resolver returns ThingEntity object with Name==null, but Name is String!;
      //   should be an error. This is server failure, normally details not reported to client
      //   but in this case server explains some details - I think it is safe, and helpful
      query = @"
    query {
       getInvalidThing {id name}
    } ";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected error");
      var errMsg = resp.Errors[0].Message;
      Assert.AreEqual("Server error: resolver for non-nullable field 'name' returned null.", errMsg);
    }

  }
}
