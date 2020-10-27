using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {

  [Query]
  interface IThingsQuery {
    [Resolver(nameof(ThingsResolvers.GetThings))]
    List<Thing_> Things { get; }

    Thing_ GetThing(int id);

    int WaitForPositiveValueAsync();

    TheFlags GetFlags();

    string EchoInputValues(bool boolVal, int intVal, float floatVal, string strVal, ThingKind kindVal);

    string EchoInputValuesWithNulls(bool? boolVal, long? longVal, double? doubleVal, [Null] string strVal,
       [DeprecatedDir("KindVal is deprecated")] ThingKind? kindVal, // test of @deprecated directive
       TheFlags? flags);

    string EchoIntArray(int[] intVals);

    string EchoEnumArray(TheFlags? flagVals);

    string EchoInputObj(InputObj inpObj);

    string EchoInputObjWithEnums(InputObjWithEnums inpObj);

    string EchoCustomScalars(decimal dec, Guid uuid);

    string EchoDateTimeScalars(DateTime dt, DateTime date, TimeSpan time);

    int[][] GetIntListRank2();

    string EchoIntListRank2(int[][] values);

    Thing[] GetThingsList();

    Thing[][] GetThingsListRank2();

    // test of sending, receiving Flag sets
    TheFlags EchoFlags(TheFlags? flags);

    string EchoFlagsStr(TheFlags? flags);

    IList<ThingsUnion> GetThingsUnionList();

    IList<INamedObj> GetSomeNamedObjects();
  }
}
