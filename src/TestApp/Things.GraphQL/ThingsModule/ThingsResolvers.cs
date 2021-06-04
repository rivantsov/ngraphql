using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NGraphQL.CodeFirst;

using Things.GraphQL.Types;

namespace Things.GraphQL {

  public class ThingsResolvers : IResolverClass {
    ThingsApp _app;

    void IResolverClass.BeginRequest(IRequestContext context) {
      _app = (ThingsApp) context.App;
    }

    public void EndRequest(IRequestContext context) {
    }

    /// <summary>Field description from resolver method's Xml comment 
    ///   - this comment will show up in the schema printout as a field description.</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    [ResolvesField("things", typeof(IThingsQuery))]
    public List<Thing> GetThings(IFieldContext context) {
      return _app.Things;
    }

    public Thing GetThing(IFieldContext context, int id) {
      return _app.Things.FirstOrDefault(t => t.Id == id);
    }

    public Thing GetInvalidThing(IFieldContext context) {
      // Name is declared non-null on Thing_, should result in error
      return new Thing() { Id = 100, Name = null };
    }

    // The method used in async call test. 
    // WaitValue is initially -1; the caller test method calls this method, awaits return; 
    // the returned task should be in 'Running' status.
    // The code then changes the WaitValue to positive value and waits for task to complete. 
    // This is a test that stack completely unwinds when awaiting for long-running async resolver method
    public static int WaitValue;

    public async Task<int> WaitForPositiveValueAsync(IFieldContext context) {
      while (WaitValue < 0 && !context.CancellationToken.IsCancellationRequested)
        await Task.Delay(100);
      return WaitValue;
    }

    public TheFlags GetFlags(IFieldContext context) {
      return TheFlags.FlagOne | TheFlags.FlagThree;
    }

    public string EchoInputValues(IFieldContext context, bool boolVal, int intVal, float floatVal, string strVal, 
                         ThingKind kindVal) {
      var result = string.Join("|", new object[] { boolVal, intVal, floatVal, strVal, kindVal });
      return result;
    }

    public string EchoInputValuesWithNulls(IFieldContext context,
         bool? boolVal, long? longVal, double? doubleVal, [Null] string strVal,
         [DeprecatedDir("KindVal is deprecated")] ThingKind? kindVal, //just demo of @deprecated directive
         TheFlags? flags) {
      var doubleStr = doubleVal.ToString();// $"{doubleVal:0.00}"; // cut off fraction digits
      var result = string.Join("|", new object[] { boolVal, longVal, doubleStr, strVal, kindVal, flags });
      return result;
    }

    public string EchoIntArray(IFieldContext context, int[] intVals) {
      string strInts = (intVals == null) ? null : string.Join(",", intVals);
      return strInts;
    }

    public string EchoEnumArray(IFieldContext context, TheFlags? flagVals) {
      string strFlags = flagVals?.ToString().Replace(" ", string.Empty);
      return strFlags;
    }

    public string EchoInputObject(IFieldContext context, InputObj inpObj) {
      return inpObj.ToString();
    }

    [ResolvesField(nameof(IThingsQuery.EchoInputObjWithEnums))]
    public string EchoInputObjectWithEnums(IFieldContext context, InputObjWithEnums inpObj) {
      return inpObj.ToString();
    }

    public string EchoCustomScalars(IFieldContext context, decimal dec, Guid uuid) {
      return string.Join("|", dec, uuid);
    }

    public string EchoDateTimeScalars(IFieldContext context, DateTime dt, DateTime date, TimeSpan time) {
      return string.Join("|", dt.ToString("yyyy-MM-dd"), date.ToString("yyyy-MM-dd"), time);
    }

    // Demo of BATCHing functionality (aka DataLoader) - loading field values for ALL parent objects in request
    public OtherThing GetMainOtherThing(IFieldContext context, Thing parent) {
      var allParents = context.GetAllParentEntities<Thing>();
      var batchedResults = allParents.ToDictionary(p => p, p => p.MainOtherThing);
      context.SetBatchedResults(batchedResults, null);
      // return parent.MainOtherThing;
      return null; // the engine will lookup result for this field in batched results
    }

    // Batching when field is a list
    public IList<OtherThing> GetOtherThings(IFieldContext context, Thing parent) {
      var allParents = context.GetAllParentEntities<Thing>();
      var batchedResults = allParents.ToDictionary(p => p, p => p.OtherThings);
      context.SetBatchedResults(batchedResults, new List<OtherThing>());
      return null; // the engine will lookup result for this field in batched results
    }

    // For testing exceptions, thrown by resolver down deep in the data tree
    public string GetNameOrThrow(IFieldContext context, OtherThing otherThing) {
      if (otherThing.Id == 5)
        throw new Exception("Exception thrown by GetNameOrThrow.");
      return otherThing.Name;
    }

    public async Task<string> GetNameOrThrowAsync(IFieldContext context, OtherThing otherThing) {
      if (otherThing.Id == 5) // child #2 of ApiThing #1, 
        throw new Exception("Exception thrown by GetNameOrThrowAsync.");
      await Task.CompletedTask;
      return otherThing.Name;
    }

    public int[][] GetIntListRank2(IFieldContext context) {
      return new int[][] {
        new int[] {3, 2, 1 },
        new int[] {6, 5, 4 }
      };
    }

    public string EchoIntListRank2(IFieldContext context, int[][] values) {
      var all = values[0].Union(values[1]).ToList();
      return string.Join(",", all);
    }

    public Thing[] GetThingsList(IFieldContext context) {
      var things = _app.Things;
      var result = new[] { things[0], things[1] };
      return result;
    }

    public Thing[][] GetThingsListRank2(IFieldContext context) {
      var things = _app.Things;
      var result = new[] {
          new[] { things[0], things[1] },
          new[] { things[1], things[2] },
      };
      return result;
    }

    public TheFlags EchoFlags(IFieldContext context, TheFlags? flags) {
      if (flags == null)
        return TheFlags.None;
      return flags.Value;
    }

    public string EchoFlagsStr(IFieldContext context, TheFlags? flags) {
      if (flags == null)
        return TheFlags.None.ToString();
      return flags.Value.ToString().Replace(" ", ""); //remove spaces
    }

    public IList<ThingsUnion> GetThingsUnionList(IFieldContext context) {
      var list = new List<ThingsUnion>();
      list.Add(new ThingsUnion(_app.Things[0]));
      list.Add(new ThingsUnion(_app.Things[0].OtherThings[0]));
      return list;
    }

    public IList<object> GetSomeNamedObjects(IFieldContext context) {
      var list = new List<object>();
      list.Add(_app.Things[0]);
      list.Add(_app.Things[0].OtherThings[0]);
      return list;
    }

    public Thing MutateThing(IFieldContext context, int id, string newName) {
      var thing = _app.Things.First(t => t.Id == id);
      thing.Name = newName;
      return thing;
    }

    public Thing MutateThingWithValidation(IFieldContext context, int id, string newName) {
      context.AddErrorIf(id < 0, "Id value may not be negative.");
      context.AddErrorIf(string.IsNullOrEmpty(newName), "newName may not be empty."); //abort immediately if cond is true
      context.AddErrorIf(newName.Length > 10, "newName too long, max size = 10.");
      // abort exc has no error info inside, it is assumed errors are already posted to request context
      // and will be returned in response
      context.AbortIfErrors(); // throw abort exc if there were errors detected
      var thing = _app.Things.FirstOrDefault(t => t.Id == id);
      context.AbortIf(thing == null, $"Thing with Id={id} not found.", ErrorCodes.ObjectNotFound); //custom type, will be placed in error.Extensions
      // actually modify 
      thing.Name = newName;
      return thing;
    }

    public int[] GetRandoms(IFieldContext context, Thing parent,  int count = 3) {
      var rand = new Random();
      var result = Enumerable.Range(1, count).Select(i => rand.Next(100)).ToArray();
      return result; 
    }

    public ThingKind[] GetAllKinds(IFieldContext context) {
      return new[] { ThingKind.KindOne, ThingKind.KindTwo, ThingKind.KindThree };
    }

    public int ThrowAggrExc(IFieldContext context) {
      var excs = new Exception[] {
        new Exception("Error1"),
        new Exception("Error2"),
        new Exception("Error3"),
      };
      var agrExc = new AggregateException(excs);
      throw agrExc; 
    }


    // this is just a test placeholder
    public bool Subscribe(IFieldContext context, string childName) {
      return true;
    }

  }
}
