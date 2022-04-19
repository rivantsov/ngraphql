using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  public struct KeyValuePairExt {
    public KeyValuePair<string, object> KeyValue;
    public TypeRef TypeRef;

    public string Key => KeyValue.Key;
    public object Value => KeyValue.Value;

    public KeyValuePairExt(string key, object value, TypeRef typeRef) {
      KeyValue = new KeyValuePair<string, object>(key, value);
      TypeRef = typeRef;
    }
  }

  /// <summary>
  ///   Slimmed down container for output object data (key/value pairs);
  ///   Serializer treats it as a dictionary (IDictionary[object,string]).
  /// </summary>
  public class OutputObjectScope : IDictionary<string, object> {
    public static readonly IList<OutputObjectScope> EmptyList = new OutputObjectScope[] { };

    public RequestPath Path;
    public object Entity;
    public FieldContext ParentFieldContext;
    public bool Merged; // skip it on producing output, the value merged to another parent

    internal readonly List<KeyValuePairExt> KeysValuePairs = new List<KeyValuePairExt>();

    public OutputObjectScope(RequestPath path, object entity, FieldContext parentFieldContext) {
      Path = path;
      Entity = entity;
      ParentFieldContext = parentFieldContext;         
    }

    public override string ToString() {
      return Entity?.ToString() ?? "(root)";
    }

    // Here are the only 2 methods actually used 
    // method used by GraphQL engine
    internal void AddValue(string key, object value, TypeRef typeRef) {
      if (value == DBNull.Value)
        return; 
      KeysValuePairs.Add(new KeyValuePairExt(key, value, typeRef));
    }

    // method used by serializer
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
      foreach (var kv in KeysValuePairs) {
        if (kv.TypeRef.MergeMode == FieldsMergeMode.Object) {
          var scope = (OutputObjectScope) kv.KeyValue.Value;
          if (scope != null && scope.Merged)
            continue; 
        }
        yield return kv.KeyValue;
      }
    }

    // The rest of the methods are never invoked at runtime (only maybe in tests)
    // this is Add method implementing IDictionary.Add, a bit slower than AddNoCheck, but compliant with dictionary semantics
    // - duplicates are allowed
    public void Add(string key, object value) {
      this[key] = value;
    }

    // (for tests only) we do implement this accessor, but never use it, it is inefficient.
    public object this[string key] {
      get {
        if (TryGetValue(key, out var value))
          return value;
        return null;
      }
      set {
        //SetValue(key, value);
        throw new NotImplementedException();
      }
    }

    public bool TryGetValue(string key, out object value) {
      value = null; 
      var kv = KeysValuePairs.FirstOrDefault(kv => kv.KeyValue.Key == key);
      if (kv.Key == null)
        return false; // key not found; keyValue is struct so it returns default
      value = kv.Value;
      return true;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

    // we don't care about efficiency in Keys and Values methods
    public ICollection<string> Keys => KeysValuePairs.Select(kv => kv.Key).ToList();

    public ICollection<object> Values => KeysValuePairs.Select(kv => kv.Value).ToList();

    public int Count => KeysValuePairs.Count;

    public void Clear() {
      KeysValuePairs.Clear();
    }

    public bool IsReadOnly => false;

    public void Add(KeyValuePair<string, object> item) {
      throw new NotImplementedException();
    }

    public bool Contains(KeyValuePair<string, object> item) {
      throw new NotImplementedException();
    }

    public bool ContainsKey(string key) {
      return KeysValuePairs.Any(kv => kv.Key == key);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) {
      throw new NotImplementedException();
    }

    public bool Remove(string key) {
      throw new NotImplementedException();
    }

    public bool Remove(KeyValuePair<string, object> item) {
      throw new NotImplementedException();
    }

  }
}
