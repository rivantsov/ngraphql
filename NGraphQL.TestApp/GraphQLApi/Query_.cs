using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {

  [Query]
  public class Query_ {
    [Resolver(nameof(ThingsApiResolvers.GetThings))]
    public List<BizThing> Things { get; }

    public BizThing GetThing(int id) { return default; }

    public int WaitForPositiveValueAsync() { return default; }

    public TheFlags GetFlags() { return default; }

    public string EchoInputValues(bool boolVal, int intVal, float floatVal, string strVal, ThingKind kindVal) 
      { return default; }

    public string EchoInputValuesWithNulls(bool? boolVal, long? longVal, double? doubleVal, [Null] string strVal,
       [DeprecatedDir("KindVal is deprecated")] ThingKind? kindVal, //just demo of @deprecated directive
       TheFlags? flags) {
      return default;
    }

    public string EchoIntArray(int[] intVals) {  return default; }

    public string EchoEnumArray(TheFlags? flagVals) { return default; }

    public string EchoInputObj(InputObj inpObj) { return default; }

    public string EchoInputObjWithEnums(InputObjWithEnums inpObj) { return default; }

    public string EchoCustomScalars(decimal dec, Guid uuid) { return default; }

    public string EchoDateTimeScalars(DateTime dt, DateTime date, TimeSpan time) { return default; }

    public OtherBizThing GetMainOtherThing(BizThing parent) { return default; }

    // Batching when field is a list
    public IList<OtherBizThing> GetOtherThings(BizThing parent) { return default; }

    // For testing exceptions, thrown by resolver down deep in the data tree
    public string GetNameOrThrow(OtherBizThing otherThing) { return default; }

    public string GetNameOrThrowAsync(OtherBizThing otherThing) { return default; }

    public int[][] GetIntListRank2() { return default; }

    public string EchoIntListRank2(int[][] values) { return default; }

    public BizThing[] GetThingsList() { return default; }

    public BizThing[][] GetThingsListRank2() { return default; }

    // test of sending, receiving Flag sets
    public TheFlags EchoFlags(TheFlags? flags) { return default; }

    public string EchoFlagsStr(TheFlags? flags) { return default; }

    public IList<ThingsUnion> GetThingsUnionList() { return default; }

    public IList<object> GetSomeNamedObjects() { return default; }
  }
}
