using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NGraphQL.Model;
using NGraphQL.Server;
using NGraphQL.Server.Parsing;
using NGraphQL.Server.Execution;
using NGraphQL.TestApp;
using System.Threading.Tasks;
using NGraphQL.Model.Request;

namespace NGraphQL.Tests {

  public partial class ExecTests {

    [TestMethod]
    public async Task Test_Errors_Syntax() {
      TestEnv.LogTestMethodStart();

      string query;
      GraphQLResponse resp;
      GraphQLError err;

      TestEnv.LogTestDescr("syntax error, invalid character.");
      query = @"
query myQuery { 
  things { 
      name, 
      nextThing { ? name }, 
  }  
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error");
      err = resp.Errors[0];
      Assert.IsTrue(err.Message.StartsWith("Query syntax error: Invalid character: '?'."), "Invalid error message");
      var loc = err.Locations[0];
      Assert.AreEqual(5, loc.Line, "Invalid error loc line");
      Assert.AreEqual(19, loc.Column, "Invalid error loc column");

      TestEnv.LogTestDescr("syntax error, unbalanced braces.");
      query = @"
query myQuery { 
  things {  name ]          # error, should be right brace '}' here 
  }  
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error");
      err = resp.Errors[0];
      Assert.AreEqual("Query syntax error: Unmatched closing brace ']'.", err.Message);
      loc = err.Locations[0];
      Assert.AreEqual(3, loc.Line, "Invalid error loc line");
      Assert.AreEqual(18, loc.Column, "Invalid error loc column");
    }


    [TestMethod]
    public async Task Test_Errors_InvalidQuery() {
      TestEnv.LogTestMethodStart();

      string query;
      GraphQLError err;
      GraphQLResponse resp;

      TestEnv.LogTestDescr("error - unknown selection field.");
      query = @"
query myQuery { 
  things { 
      name, 
      nextThing { name, unknownField }, 
  }  
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error");
      err = resp.Errors[0];
      Assert.IsTrue(err.Message.StartsWith("Field 'unknownField' not found"), "Invalid error message");


      TestEnv.LogTestDescr("error - object-type field must have a selection subset.");
      // error - 
      query = @"
query myQuery { 
  getThing(id: 1) {
    name, 
    nextThing          # error - selection subset is missing
  } 
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error");
      err = resp.Errors[0];
      var errPath = err.Path.ToCommaText();
      Assert.AreEqual("getThing,nextThing", errPath, "Invalid error path");
      Assert.AreEqual($"Field 'nextThing' of type '{nameof(Thing_)}' must have a selection subset.", err.Message);

      TestEnv.LogTestDescr("error - scalar, enum fields may not have a selection subset.");
      query = @"
query { 
  things {  name { abc } } 
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error");
      err = resp.Errors[0];
      Assert.AreEqual("Field 'name' of type 'String' may not have a selection subset.", err.Message);

      TestEnv.LogTestDescr("error - default (anonymous) query may not be combined with other operations.");
      query = @"
# this is a default query
{ 
  things {  name { abc }, def } 
}

# this is another query
query { 
  things {  name { abc }, def } 
}
";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error");
      var errMsg = resp.Errors[0].Message;
      var expected = "If the request contains a default (anonymous) query, it cannot contain any other operations.";
      Assert.AreEqual(expected, errMsg);
    }

  }
}
