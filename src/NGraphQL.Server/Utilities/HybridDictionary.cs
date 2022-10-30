using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Utilities {

  public interface INamedObject {
    string Name { get; }
  }

  // Hybrid (list + dict), for fast lookups in list for small sets, and lookup thru dict 
  //  for large sets. Used for FieldDef list - in real world apps, there are classes/entities with 100+ fields
  public class HybridDictionary<T>: IEnumerable<T> where T: INamedObject {
    const int SwitchCount = 6;

    List<T> _list = new List<T>();
    Dictionary<string, T> _dict = new Dictionary<string, T>();

    public void Add(T obj) {
      _list.Add(obj);
      _dict.Add(obj.Name, obj); 
    }
    public void AddRange(IList<T> list) {
      foreach (var obj in list)
        Add(obj); 
    }

    public T this[string name] {
      get {
        if (TryGetValue(name, out var value))
          return value;
        return default; 
      } //get
    }

    public bool TryGetValue(string name, out T value) {
      if (_list.Count < SwitchCount) {
        // use list search
        for (int i = 0; i < _list.Count; i++) {
          var obj = _list[i];
          if (obj.Name == name) {
            value = obj;
            return true;
          }
        } // for i
        value = default; 
        return false;
      } else {
        // use dict
        return _dict.TryGetValue(name, out value);
      } //if      
    }

    public IEnumerator<T> GetEnumerator() {
      return _list.GetEnumerator(); 
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return _list.GetEnumerator(); 
    }

    public int Count => _list.Count; 
    public T this[int index] {
      get {
        return _list[index]; 
      }
    }
  }
}
