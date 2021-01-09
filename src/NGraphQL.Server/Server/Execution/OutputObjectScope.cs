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

    public RequestPath Path; 
    public readonly IFieldContext SourceFieldContext;
    public ObjectTypeDef MappedTypeDef; // when field is union or interface
    public object Entity;

    public IList<MappedField> Fields { get; private set; }
    object[] _values;
    IBitSet _valuesMask; //indicates if there's a value

    // creates root scope
    public OutputObjectScope() {
      Path = new RequestPath(); 
    }

    public OutputObjectScope(IFieldContext sourceFieldContext, RequestPath path, object entity) {
      SourceFieldContext = sourceFieldContext;
      Path = path; 
      Entity = entity;
    }
    public override string ToString() {
      return Entity?.ToString() ?? "(root)";
    }

    internal void Init(ObjectTypeDef objectTypeDef, IList<MappedField> fields) {
      MappedTypeDef = objectTypeDef;
      Fields = fields;
      _values = new object[fields.Count];
      _valuesMask = BitSet.Create(fields.Count);
    }

    // Here are the only 2 methods actually used 
    // method used by GraphQL engine
    internal void SetValue(int index, object value) {
      _values[index] = value;
      _valuesMask.SetValue(index, true);
    }

    internal bool HasValue(int index) {
      return _valuesMask.GetValue(index);
    }

    internal object GetValue(int index) {
      return _values[index];
    }

    // method used by serializer
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
      for(int i = 0; i < Fields.Count; i++) {
        if (_valuesMask.GetValue(i))
          yield return new KeyValuePair<string, object>(Fields[i].Field.Key, _values[i]);
      }
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
        var index = IndexOf(key);
        if(index >= 0)
          SetValue(index, value); 
        else 
          throw new Exception($"Key {key} is not valid for this scope.");
      }
    }

    public bool TryGetValue(string key, out object value) {
      value = null;
      var index = IndexOf(key);
      if(index < 0)
        return false;
      value = _values[index];
      return true; 
    }

    private int IndexOf(string key) {
      for(int i = 0; i < Fields.Count; i++) {
        if (Fields[i].Field.Key == key)
          return i; 
      }
      return -1; 
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return this.GetEnumerator();
    }

    // we don't care about efficiency in Keys and Values methods
    public ICollection<string> Keys => Fields.Select(f => f.Field.Key).ToList();

    public ICollection<object> Values => _values;

    public int Count => _values.Length;

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
      return Fields.Any(f => f.Field.Key == key);
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
