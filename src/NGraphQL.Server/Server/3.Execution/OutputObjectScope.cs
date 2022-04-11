using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Execution {

  /// <summary>
  ///   Slimmed down container for output object data (key/value pairs);
  ///   Serializer treats it as a dictionary (IDictionary[object,string]).
  /// </summary>
  public class OutputObjectScope : IDictionary<string, object> {
    public static readonly IList<OutputObjectScope> EmptyList = new OutputObjectScope[] { };

    public RequestPath Path;
    public object Entity;
    public ObjectTypeMapping Mapping;
    public bool IsMerged; 

    List<KeyValuePair<string, object>> _keysValues = new List<KeyValuePair<string, object>>();

    public OutputObjectScope(RequestPath path, object entity, ObjectTypeMapping mapping) {
      Path = path;
      Entity = entity;
      Mapping = mapping;
    }
    public override string ToString() {
      return Entity?.ToString() ?? "(root)";
    }

    // Here are the only 2 methods actually used 
    // method used by GraphQL engine
    internal void AddValue(string key, object value) {
      if (value == DBNull.Value)
        return; 
      _keysValues.Add(new KeyValuePair<string, object>(key, value));
    }

    // method used by serializer
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
      foreach (var kv in _keysValues)
        yield return kv; 
    }

    public void AddFrom(OutputObjectScope other) {
      _keysValues.AddRange(other._keysValues);
    }

    // The rest of the methods are never invoked at runtime (only maybe in tests)
    // this is Add method implementing IDictionary.Add, a bit slower than AddNoCheck, but compliant with dictionary semantics
    // - duplicates are allowed
    public void Add(string key, object value) {
      throw new NotImplementedException();
    }


    // we do implement this accessor, but never use it, it is inefficient.
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
      var kv = _keysValues.FirstOrDefault(kv => kv.Key == key);
      if (kv.Key == null)
        return false; // key not found; keyValue is struct so it returns default
      value = kv.Value;
      return true;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

    // we don't care about efficiency in Keys and Values methods
    public ICollection<string> Keys => _keysValues.Select(kv => kv.Key).ToList();

    public ICollection<object> Values => _keysValues.Select(kv => kv.Value).ToList();

    public int Count => _keysValues.Count;

    public bool IsReadOnly => false;

    public void Add(KeyValuePair<string, object> item) {
      throw new NotImplementedException();
    }

    public void Clear() {
      throw new NotImplementedException();
    }

    public bool Contains(KeyValuePair<string, object> item) {
      throw new NotImplementedException();
    }

    public bool ContainsKey(string key) {
      return _keysValues.Any(kv => kv.Key == key);
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
