using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NGraphQL.CodeFirst;
using NGraphQL.Model;
using NGraphQL.Model.Request;
using NGraphQL.Utilities;

namespace NGraphQL.Server.Execution {

  /// <summary>
  ///   Slimmed down container for output object data (key/value pairs);
  ///   Serializer treats it as a dictionary (IDictionary[object,string]).
  /// </summary>
  public class OutputObjectScope : IDictionary<string, object> {
    public static readonly IList<OutputObjectScope> EmptyList = new OutputObjectScope[] { };

    public readonly SelectionField SourceField;
    public RequestPath Path;
    public object Entity;
    public ObjectTypeMapping Mapping;

    IList<KeyValuePair<string, object>> _keysValues = new List<KeyValuePair<string, object>>();
    HashSet<string> _keys = new HashSet<string>(); 

    // creates root scope
    public OutputObjectScope() {
      Path = new RequestPath();
    }

    public OutputObjectScope(SelectionField sourceField, RequestPath path, object entity, ObjectTypeMapping mapping) {
      SourceField = sourceField;
      Path = path;
      Entity = entity;
      Mapping = mapping; 
    }
    public override string ToString() {
      return Entity?.ToString() ?? "(root)";
    }

    // Here are the only 2 methods actually used 
    // method used by GraphQL engine
    internal void SetValue(string key, object value) {
      if (!_keys.Add(key)) 
        _keysValues.Add(new KeyValuePair<string, object>(key, value));
    }

    // method used by serializer
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
      return _keysValues.GetEnumerator(); 
    }

    // The rest of the methods are never invoked at runtime (only maybe in tests)
    // this is Add method implementing IDictionary.Add, a bit slower than AddNoCheck, but compliant with dictionary semantics
    // - duplicates are allowed
    public void Add(string key, object value) {
      this[key] = value;
    }

    // we do implement this accessor, but never use it, it is inefficient.
    public object this[string key] {
      get {
        if (TryGetValue(key, out var value))
          return value;
        return null;
      }
      set {
          SetValue(key, value);
      }
    }

    public bool TryGetValue(string key, out object value) {
      value = null; 
      if (!_keys.Contains(key))
        return false;
      var kv = _keysValues.First(kv => kv.Key == key);
      value = kv.Value;
      return true;
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

    // we don't care about efficiency in Keys and Values methods
    public ICollection<string> Keys => _keys;

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
      return _keys.Contains(key);
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
