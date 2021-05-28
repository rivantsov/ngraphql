using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NGraphQL.Utilities;
using NGraphQL.Model;
using NGraphQL.Server;
using NGraphQL.Server.Execution;
using System.Collections;
using NGraphQL.TestApp;

namespace NGraphQL.Tests {
  using TDict = Dictionary<string, object>;

  public partial class ExecTests {

    [TestMethod]
    public async Task Test_Input_InputTypes() {
      TestEnv.LogTestMethodStart();
      string query;

      TestEnv.LogTestDescr("input values of various types.");
      query = @"
query { 
  res: echoInputValues(boolVal: true, intVal: 123, floatVal: 23.45, strVal: ""abc"", kindVal: KIND_ONE)
}";
      var resp = await ExecuteAsync(query);
      var result = (string)resp.Data["res"];
      Assert.AreEqual("True|123|23.45|abc|KindOne", result, "Result mismatch");

      TestEnv.LogTestDescr("nullable input values of various types.");
      query = @"
query { 
  res: echoInputValuesWithNulls(boolVal: true, longVal: 123, doubleVal: 23.45, strVal: null, kindVal: KIND_ONE, flags: [FLAG_ONE])
}";
      resp = await ExecuteAsync(query);
      result = (string)resp.Data["res"];
      Assert.AreEqual("True|123|23.45||KindOne|FlagOne", result, "Result mismatch");

      TestEnv.LogTestDescr("hex notation for integer literals (0xFF).");
      query = @"
query { 
  res: echoInputValuesWithNulls(boolVal: true, longVal: 0xFF, doubleVal: 23.45, strVal: null, kindVal: KIND_ONE, flags: [FLAG_ONE])
}";
      resp = await ExecuteAsync(query);
      result = (string)resp.Data["res"];
      Assert.AreEqual("True|255|23.45||KindOne|FlagOne", result, "Result mismatch");
    }

    [TestMethod]
    public async Task Test_Input_InputObjects() {
      string query;
      GraphQLResponse resp;
      string result;
      TDict vars;
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr("literal input object as argument.");
      query = @"
query { 
  res: echoInputObj(inpObj: {id: 1, name: ""abc"", num: 0})       # returns inpObj.ToString()
}";
      resp = await ExecuteAsync(query);
      result = (string)resp.Data["res"];
      Assert.AreEqual("id:1,name:abc,num:0", result, "Result mismatch");

      TestEnv.LogTestDescr("literal input object as argument, with some of its properties set from variables.");
      query = @"
query myQuery($id : int) { 
  res: echoInputObj(inpObj: {id: $id, name: ""abc"", num:456})
}";
      vars = new TDict() { { "id", 1 } };
      resp = await ExecuteAsync(query, vars);
      result = (string)resp.Data["res"];
      Assert.AreEqual("id:1,name:abc,num:456", result, "Result mismatch");

      TestEnv.LogTestDescr("literal input object as argument; variable value ($id) is not provided and is assigned from default.");
      query = @"
query myQuery($num: Int!, $name: String!, $id: Int = 123) { 
  echoInputObj (inpObj: {id: $id, num: $num, name: $name}) 
}";
      vars = new TDict();
      vars["num"] = 456;
      vars["name"] = "SomeName";
      resp = await ExecuteAsync(query, vars);
      var echoInpObj2 = resp.GetValue<string>("echoInputObj");
      Assert.AreEqual("id:123,name:SomeName,num:456", echoInpObj2); //this is InputObj.ToString()

      TestEnv.LogTestDescr("complex input object in a variable.");
      query = @"
query myQuery($inpObj: InputObj!) { 
  echoInputObj (inpObj: $inpObj) 
}";
      vars = new TDict();
      // we cannot use InputObj here, serializer will send first-cap prop names and request will fail
      vars["inpObj"] = new InputObj() { Id = 123, Num = 456, Name = "SomeName" };
      resp = await ExecuteAsync(query, vars);
      var echoInpObj = resp.GetValue<string>("echoInputObj");
      Assert.AreEqual("id:123,name:SomeName,num:456", echoInpObj); //this is InputObj.ToString()
    }

    [TestMethod]
    public async Task Test_Input_Variables() {
      TestEnv.LogTestMethodStart();
      string query;
      TDict vars;
      GraphQLResponse resp;
      string echoResp;

      TestEnv.LogTestDescr("missing variable value.");
      query = @"
query myQuery($longVal: Long!) { 
  echo: echoInputValuesWithNulls (longVal: $longVal)
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error(s)");
      Assert.AreEqual("Value for required variable longVal is not provided.", resp.Errors[0].Message);

      TestEnv.LogTestDescr("unknown variable used in argument.");
      query = @"
query myQuery($longVal: Long) { 
  echo: echoInputValuesWithNulls (longVal: $longValXYZ)
}";
      vars = new TDict() { { "longVal", 654321 } };
      resp = await ExecuteAsync(query, vars, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 error(s)");
      Assert.AreEqual("Variable $longValXYZ not defined.", resp.Errors[0].Message);

      TestEnv.LogTestDescr("variable value type mismatch - sending string value for a long var.");
      query = @"
query myQuery($longVal: Long) { 
  echo: echoInputValuesWithNulls ()
}";
      vars = new TDict() { { "longVal", "abc" } };
      resp = await ExecuteAsync(query, vars, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "expected 1 error");
      Assert.AreEqual("Variable $longVal: failed to convert value 'abc' to type Long: Invalid Long value: 'abc'",
        resp.Errors[0].Message);

      TestEnv.LogTestDescr("string -> enum conversion; enum input values are sent as strings.");
      query = @"
query myQuery($kindVal: ThingKind) { 
  echo: echoInputValuesWithNulls (kindVal: $kindVal)
}";
      vars = new TDict() { { "kindVal", "KIND_ONE" } };
      resp = await ExecuteAsync(query, vars);
      echoResp = resp.GetValue<string>("echo");
      Assert.AreEqual("||||KindOne|", echoResp);

      TestEnv.LogTestDescr("int -> long? automatic conversion.");
      query = @"
query myQuery($longVal: Long) { 
  echo: echoInputValuesWithNulls (longVal: $longVal)
}";
      vars = new TDict() { { "longVal", 654321 } };
      resp = await ExecuteAsync(query, vars);
      echoResp = resp.GetValue<string>("echo");
      Assert.AreEqual("|654321||||", echoResp);

      TestEnv.LogTestDescr("multiple input variables of various types, happy path.");
      query = @"
query myQuery($boolVal: Boolean, $longVal: Long, $doubleVal: Double, $strVal: String, $kindVal: ThingKind, $flags: [TheFlags!]) { 
  echo: echoInputValuesWithNulls (boolVal: $boolVal, longVal: $longVal, doubleVal: $doubleVal, strVal: $strVal, 
                                  kindVal: $kindVal, flags: $flags )
}";
      vars = new TDict() {
        { "boolVal", true }, { "longVal", 654321 }, { "doubleVal", 543.21 },
        { "kindVal", "KIND_ONE" }, {"flags", new string[] {"FLAG_ONE", "FLAG_TWO"}},
        { "strVal", "SomeString" }
      };
      resp = await ExecuteAsync(query, vars);
      echoResp = resp.GetValue<string>("echo");
      Assert.AreEqual("True|654321|543.21|SomeString|KindOne|FlagOne, FlagTwo", echoResp); //this is InputObj.ToString()
    }

    [TestMethod]
    public async Task Test_Input_FieldArgs() {
      TestEnv.LogTestMethodStart();
      string query;
      TDict vars;
      GraphQLResponse resp;
      GraphQLError err;

      TestEnv.LogTestDescr("error - invalid arg names.");
      query = @"
query { 
  echo: echoInputValuesWithNulls(boolValXYZ: true, longValXYZ: 123, doubleVal: 23.45)
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.IsTrue(resp.Errors.Count == 2, "Expected errors");
      err = resp.Errors[0];
      Assert.AreEqual("Field(dir) 'echoInputValuesWithNulls': argument 'boolValXYZ' not defined.", err.Message);
      Assert.AreEqual(2, err.Path.Count, "Expected 2 elem path");
      Assert.IsTrue(err.Locations != null && err.Locations.Count > 0, "Expected error location.");

      TestEnv.LogTestDescr("error - args referring to vars of wrong type.");
      query = @"
query ($str: String) { 
  echo: echoInputValuesWithNulls(longVal: $str)
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.IsTrue(resp.Errors.Count == 1, "Expected errors");
      err = resp.Errors[0];
      Assert.AreEqual("Incompatible types: variable $str cannot be converted to type 'Long'", err.Message);

      TestEnv.LogTestDescr("error - invalid arg name for a directive.");
      query = @"
query { 
  things { name @include(ifXYZ: true) }
}";
      resp = await ExecuteAsync(query, throwOnError: false);
      Assert.IsTrue(resp.Errors.Count == 1, "Expected 1 error(s)");
      Assert.AreEqual("Field(dir) 'include': argument 'ifXYZ' not defined.", resp.Errors[0].Message);

      TestEnv.LogTestDescr("error - passing wrong value types to field args using a literal.");
      query = @"
query myQuery() { 
  echo: echoInputValuesWithNulls (longVal: ""abc"")
}";
      vars = new TDict() { };
      resp = await ExecuteAsync(query, vars, throwOnError: false);
      Assert.AreEqual(1, resp.Errors.Count, "Expected 1 errors");
      err = resp.Errors[0];
      Assert.AreEqual("Invalid long value: '\"abc\"'", err.Message);

      TestEnv.LogTestDescr("error - passing wrong value types to field args, using bad value in a variable.");
      query = @"
query myQuery($longVal: Long, $doubleVal: Double) { 
  echo: echoInputValuesWithNulls (longVal: $longVal, doubleVal: $doubleVal)
}";
      vars = new TDict() { { "longVal", true }, { "doubleVal", "abc" } };
      resp = await ExecuteAsync(query, vars, throwOnError: false);
      Assert.AreEqual(2, resp.Errors.Count, "Expected 2 errors");

      TestEnv.LogTestDescr("error - passing wrong value types to field args, using variables; test multiple errors.");
      query = @"
query myQuery($boolVal: Boolean, $longVal: Long, $doubleVal: Double, $strVal: String, $kindVal: ThingKind, $flags: [TheFlags!]) { 
  # totally wrong match of args, expected 3 errors
  echo: echoInputValuesWithNulls (boolVal: $longVal, longVal: $doubleVal, doubleVal: $strVal ) 
}";
      vars = new TDict() {
        { "boolVal", true }, { "longVal", 654321 }, { "doubleVal", 543.21 },
        { "kindVal", "KIND_ONE" }, {"flags", new string[] {"FLAG_ONE", "FLAG_TWO"}},
        { "strVal", "SomeString" }
      };
      resp = await ExecuteAsync(query, vars, throwOnError: false);
      Assert.AreEqual(3, resp.Errors.Count, "Expected 3 error(s)");
    }

    [TestMethod]
    public async Task Test_Input_Lists() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;
      TDict vars; 

      TestEnv.LogTestDescr("passing a list as an argument (list as literal).");
      query = @"
query { 
  res: echoIntArray(intVals: [3,2,1])
}";
      resp = await ExecuteAsync(query);
      var result = resp.Data.GetValue<string>("res");
      Assert.AreEqual("3,2,1", result, "Result mismatch");

      TestEnv.LogTestDescr("passing an enum array in argument (mapped to c# enum with [Flags] attr).");
      query = @"
query { 
  res: echoEnumArray(flagVals: [FLAG_ONE, FLAG_TWO])              # returns flagVals.ToString()
}";
      resp = await ExecuteAsync(query);
      result = resp.Data.GetValue<string>("res");
      Assert.AreEqual("FlagOne,FlagTwo", result, "Result mismatch");

      TestEnv.LogTestDescr("passing a list int[][], value from literal and variables.");
      query = @"
query ($one: Int) { 
  res: echoIntListRank2(values: [ [3, 2, $one], [6, 5, 4] ] )
}";
      vars = new TDict() { { "one", 1 } };
      resp = await ExecuteAsync(query, vars);  
      var str = resp.GetValue<string>("res");
      Assert.AreEqual("3,2,1,6,5,4", str, "Result mismatch");
    }

    [TestMethod]
    public async Task Test_Input_Nulls() {
      TestEnv.LogTestMethodStart();
      string query;
      GraphQLResponse resp;
      string result;

      TestEnv.LogTestDescr("Nullable args are optional by definition; calling method with all (nullable) args missing");
      query = @"
query { 
  echo: echoInputValuesWithNulls()
}";
      resp = await ExecuteAsync(query);
      result = resp.Data.GetValue<string>("echo");
      Assert.AreEqual("|||||", result, "Result mismatch");

      TestEnv.LogTestDescr("value coersion.");
      query = @"
query { 
  echo: echoInputValuesWithNulls(boolVal: true, longVal: 123, doubleVal: 23, strVal: null, kindVal: KIND_ONE, flags: [FLAG_ONE])
}";
      resp = await ExecuteAsync(query);
      result = resp.Data.GetValue<string>("echo");
      Assert.AreEqual("True|123|23||KindOne|FlagOne", result, "Result mismatch");
    }

    [TestMethod]
    public async Task Test_Input_FlagsEnum() {
      TestEnv.LogTestMethodStart();
      var enumValues = Enum.GetValues(typeof(ThingKind));

      TestEnv.LogTestDescr("passing flags enum value as literal array.");
      var query = @" query { echoFlags(flags: [FLAG_ONE FLAG_THREE]) }";
      var resp = await ExecuteAsync(query);
      var theFlags = resp.GetValue<string[]>("echoFlags");
      var theFlagsStr = string.Join(",", theFlags);
      Assert.AreEqual("FLAG_ONE,FLAG_THREE", theFlagsStr, "Invalid flags value");

      TestEnv.LogTestDescr("passing empty array as flags enum value.");
      query = @" query { echoFlags(flags: []) }";
      resp = await ExecuteAsync(query);
      theFlags = resp.GetValue<string[]>("echoFlags");
      theFlagsStr = string.Join(",", theFlags);
      Assert.AreEqual("", theFlagsStr, "Expected empty flags value");

      TestEnv.LogTestDescr("passing null as flags enum value; should return empty array.");
      query = @" query { echoFlags(flags: null) }";
      resp = await ExecuteAsync(query);
      theFlags = resp.GetValue<string[]>("echoFlags");
      theFlagsStr = string.Join(",", theFlags);
      Assert.AreEqual("", theFlagsStr, "Expected empty flags value");

      TestEnv.LogTestDescr(" verify flags enum value passed to resolver.");
      // Test - verify value that arrives to resolver
      query = @" query { echoFlagsStr(flags: [FLAG_TWO FLAG_THREE]) }    # returns c# value.ToString() ";
      resp = await ExecuteAsync(query);
      theFlagsStr = resp.GetValue<string>("echoFlagsStr");
      Assert.AreEqual("FlagTwo,FlagThree", theFlagsStr, "Invalid returned flags value");


      TestEnv.LogTestDescr(" testing flags enum field on object. ApiThing.theFlags is [Flags] enum; the value returned should be list of strings.");
      query = @" query {  thing: getThing(id:1) { id name kind theFlags } }";
      resp = await ExecuteAsync(query);
      theFlags = resp.GetValue<string[]>("thing.theFlags");
      theFlagsStr = string.Join(",", theFlags);
      Assert.AreEqual("FLAG_ONE,FLAG_THREE", theFlagsStr, "Invalid enum array property");
    }

    [TestMethod]
    public async Task Test_Input_Validation() {
      TestEnv.LogTestMethodStart();
      string query;
      TDict vars;
      GraphQLResponse resp;

      TestEnv.LogTestDescr("validation of input values in resolver code, posting response errors and aborting the request.");
      query = @"
mutation ($id: Int!, $newName: String!) { 
  th: mutateThingWithValidation(id: $id, newName: $newName) { 
         id, name 
      }
}";
      vars = new TDict() { { "id", -1 }, { "newName", "Name  Tooo  Loooooooooooooooooooooong" } };
      resp = await ExecuteAsync(query, vars, throwOnError: false);
      Assert.AreEqual(2, resp.Errors.Count, "expected errors");
      Assert.AreEqual("Id value may not be negative.", resp.Errors[0].Message);
      Assert.AreEqual("newName too long, max size = 10.", resp.Errors[1].Message);

      TestEnv.LogTestDescr("same resolver method, different variable values, happy path - all values are OK.");
      var newName2 = "Name2_";
      vars = new TDict() { { "id", 2 }, { "newName", newName2 } };
      resp = await ExecuteAsync(query, vars, throwOnError: false);
      Assert.AreEqual(0, resp.Errors.Count, "expected no errors");
      var thing2 = ThingsApp.Instance.Things.First(t => t.Id == 2);
      Assert.AreEqual(newName2, thing2.Name);

      // undo the change 
      thing2.Name = "Name2";
    }

  }
}
