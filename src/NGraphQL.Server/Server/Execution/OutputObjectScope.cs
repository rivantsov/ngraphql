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

    public readonly MappedSelectionField SourceField;
    public readonly object Entity;
    public readonly RequestPath Path;

    public readonly ObjectTypeDef TypeDef;

    IDictionary<string, object> _values = new Dictionary<string, object>();


    // creates root scope
    public OutputObjectScope() {
      Path = new RequestPath(); 
    }

    public OutputObjectScope(MappedSelectionField sourceField, ObjectTypeDef typeDef, 
                           object entity, RequestPath path) {
      SourceField = sourceField;
      TypeDef = typeDef;
      Path = path; 
      Entity = entity;
    }

    public override string ToString() {
      return Entity?.ToString() ?? "(root)";
    }

    // Method used for top scope and top selection fields;
    // top fields are executed in parallel, so we must protect from concurrent access
    //  when we set the result value
    private object _lock = new object(); 
    internal void SetValueSafe(string name, object value) {
      lock(_lock)
        _values[name] = value;
    }

    internal bool HasValue(string name) {
      return _values.ContainsKey(name);
    }

    // method used by serializer
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
      return _values.GetEnumerator();
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
        if(TryGetValue(key, out var value))
          return value;
        return null; 
      }
      set {
        _values[key] = value;  
      }
    }

    public bool TryGetValue(string key, out object value) {
      return _values.TryGetValue(key, out value); 
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

    public ICollection<string> Keys => _values.Keys;

    public ICollection<object> Values => _values.Values;

    public int Count => _values.Count;

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
      return _values.ContainsKey(key);
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
