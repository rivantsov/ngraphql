﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;
using NGraphQL.Server;

namespace NGraphQL.TestApp {

  public class ThingsApiResolvers : IResolverClass {
    ThingsApp _app;

    void IResolverClass.BeginRequest(IRequestContext context) {
      _app = ((ThingsApi)context.Server.Api).App;
    }

    public void EndRequest(IRequestContext context) {
    }

    /// <summary>Field description from resolver method's Xml comment 
    ///   - this comment will show up in the schema printout as a field description.</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [Query("things")]
    public List<BizThing> GetThings(IFieldContext context) {
      return _app.Things;
    }

    [Query("getThing"), Null]
    public BizThing GetThing(IFieldContext context, int id) {
      return _app.Things.FirstOrDefault(t => t.Id == id);
    }

    [Query]
    public async Task<int> WaitForPositiveValueAsync(IFieldContext context) {
      return await _app.WaitForPositiveValueAsync(context.CancellationToken);
    }

    [Query]
    public TheFlags GetFlags(IFieldContext context) {
      return TheFlags.FlagOne | TheFlags.FlagThree;
    }

    [Query]
    public string EchoInputValues(IFieldContext context, bool boolVal, int intVal, float floatVal, string strVal, ThingKind kindVal) {
      var result = string.Join("|", new object[] { boolVal, intVal, floatVal, strVal, kindVal });
      return result;
    }

    [Query]
    public string EchoInputValuesWithNulls(IFieldContext context,
         bool? boolVal, long? longVal, double? doubleVal, [Null] string strVal,
         [DeprecatedDir("KindVal is deprecated")] ThingKind? kindVal, //just demo of @deprecated directive
         TheFlags? flags) {
      var doubleStr = doubleVal.ToString();// $"{doubleVal:0.00}"; // cut off fraction digits
      var result = string.Join("|", new object[] { boolVal, longVal, doubleStr, strVal, kindVal, flags });
      return result;
    }

    [Query]
    public string EchoIntArray(IFieldContext context, int[] intVals) {
      string strInts = (intVals == null) ? null : string.Join(",", intVals);
      return strInts;
    }

    [Query]
    public string EchoEnumArray(IFieldContext context, TheFlags? flagVals) {
      string strFlags = flagVals?.ToString().Replace(" ", string.Empty);
      return strFlags;
    }

    [Query]
    public string EchoInputObj(IFieldContext context, InputObj inpObj) {
      return inpObj.ToString();
    }

    [Query]
    public string EchoInputObjWithEnums(IFieldContext context, InputObjWithEnums inpObj) {
      return inpObj.ToString();
    }

    [Query]
    public string EchoCustomScalars(IFieldContext context, decimal dec, Guid uuid) {
      return string.Join("|", dec, uuid);
    }
    [Query]
    public string EchoDateTimeScalars(IFieldContext context, DateTime dt, DateTime date, TimeSpan time) {
      return string.Join("|", dt.ToString("yyyy-MM-dd"), date.ToString("yyyy-MM-dd"), time);
    }

    // Demo of BATCHing functionality (aka DataLoader) - loading field values for ALL parent objects in request

    [Field("mainOtherThing", OnType = typeof(ApiThing))]
    public OtherBizThing GetMainOtherThing(IFieldContext context, BizThing parent) {
      var allParents = context.GetAllParentEntities<BizThing>();
      var batchedResults = allParents.ToDictionary(p => p, p => p.MainOtherThing);
      context.SetBatchedResults(batchedResults);
      return parent.MainOtherThing;
    }

    // Batching when field is a list
    [Field("otherThings", OnType = typeof(ApiThing))]
    public IList<OtherBizThing> GetOtherThings(IFieldContext context, BizThing parent) {
      var allParents = context.GetAllParentEntities<BizThing>();
      var batchedResults = allParents.ToDictionary(p => p, p => p.OtherThings);
      context.SetBatchedResults(batchedResults);
      return batchedResults[parent];
    }

    // For testing exceptions, thrown by resolver down deep in the data tree
    [Field("getNameOrThrow", OnType = typeof(ApiOtherThing))]
    public string GetNameOrThrow(IFieldContext context, OtherBizThing otherThing) {
      if (otherThing.Id == 5)
        throw new Exception("Exception thrown by GetNameOrThrow.");
      return otherThing.Name;
    }

    [Field("getNameOrThrowAsync", OnType = typeof(ApiOtherThing))]
    public async Task<string> GetNameOrThrowAsync(IFieldContext context, OtherBizThing otherThing) {
      if (otherThing.Id == 5) // child #2 of ApiThing #1, 
        throw new Exception("Exception thrown by GetNameOrThrowAsync.");
      await Task.CompletedTask;
      return otherThing.Name;
    }

    [Query] // test of returning list of list
    public int[][] GetIntListRank2(IFieldContext context) {
      return new int[][] {
        new int[] {3, 2, 1 },
        new int[] {6, 5, 4 }
      };
    }

    [Query] // test of returning list of list
    public string EchoIntListRank2(IFieldContext context, int[][] values) {
      var all = values[0].Union(values[1]).ToList();
      return string.Join(",", all);
    }

    [Query] // test of returning list of list
    public BizThing[] GetThingsList(IFieldContext context) {
      var things = _app.Things;
      var result = new[] { things[0], things[1] };
      return result;
    }

    [Query] // test of returning list of list
    public BizThing[][] GetThingsListRank2(IFieldContext context) {
      var things = _app.Things;
      var result = new[] {
          new[] { things[0], things[1] },
          new[] { things[1], things[2] },
      };
      return result;
    }

    [Query] // test of sending, receiving Flag sets
    public TheFlags EchoFlags(IFieldContext context, TheFlags? flags) {
      if (flags == null)
        return TheFlags.None;
      return flags.Value;
    }

    [Query] // test of sending, receiving Flag sets
    public string EchoFlagsStr(IFieldContext context, TheFlags? flags) {
      if (flags == null)
        return TheFlags.None.ToString();
      return flags.Value.ToString().Replace(" ", ""); //remove spaces
    }

    [Query]
    public IList<ThingsUnion> GetThingsUnionList(IFieldContext context) {
      var list = new List<ThingsUnion>();
      list.Add(new ThingsUnion(_app.Things[0]));
      list.Add(new ThingsUnion(_app.Things[0].OtherThings[0]));
      return list;
    }

    [Query]
    public IList<InterfaceBox<INamedObj>> GetSomeNamedObjects(IFieldContext context) {
      var list = new List<InterfaceBox<INamedObj>>();
      list.Add(new InterfaceBox<INamedObj>(_app.Things[0]));
      list.Add(new InterfaceBox<INamedObj>(_app.Things[0].OtherThings[0]));
      return list;
    }

    // this is just a test placeholder
    [Subscription]
    public bool Subscribe(IFieldContext context, string childName) {
      return true;
    }

    [Mutation]
    public BizThing MutateThing(IFieldContext context, int id, string newName) {
      var thing = _app.Things.First(t => t.Id == id);
      thing.Name = newName;
      return thing;
    }

    [Mutation]
    public BizThing MutateThingWithValidation(IFieldContext context, int id, string newName) {
      context.AddErrorIf(id < 0, "Id value may not be negative.");
      context.AddErrorIf(string.IsNullOrEmpty(newName), "newName may not be empty."); //abort immediately if cond is true
      context.AddErrorIf(newName.Length > 10, "newName too long, max size = 10.");
      // abort exc has no error info inside, it is assumed errors are already posted to request context
      // and will be returned in response
      context.AbortIfErrors(); // throw abort exc if there were errors detected
      var thing = _app.Things.FirstOrDefault(t => t.Id == id);
      context.AbortIf(thing == null, $"Thing with Id={id} not found.", ErrorTypes.ObjectNotFound); //custom type, will be placed in error.Extensions
      // actually modify 
      thing.Name = newName;
      return thing;
    }

  }


}
