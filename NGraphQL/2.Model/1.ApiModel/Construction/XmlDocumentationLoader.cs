using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using NGraphQL.Model;

namespace NGraphQL.Model.Construction {

  public class XmlDocumentationLoader {
    Dictionary<string, Dictionary<string, XElement>> _data = new Dictionary<string, Dictionary<string, XElement>>(); 

    public string GetDocString(object target, Type declaringType) {
      if(target == null)
        throw new Exception("Target may not be null");
      var asmName = declaringType.Assembly.GetName().Name;
      if(!_data.TryGetValue(asmName, out var dict)) {
        // we are seeing this assembly for the first time; try loading it
        if (!TryLoadAssemblyXmlFile(declaringType.Assembly))
          return null;
        // try again
        _data.TryGetValue(asmName, out dict);
      }
      //
      if (dict == null) // the assembly has no xml file
        return null;
      var key = GetKey(target);
      if(!dict.TryGetValue(key, out var member))
        return null;
      var str = member.Element("summary").Value?.Trim();
      return str; 
    }

    private string GetKey(object obj) {
      switch(obj) {
        case Type t: return $"T:{t.Namespace}.{t.Name}";
        case FieldInfo f: return $"F:{FullName(f.DeclaringType)}.{f.Name}";
        case PropertyInfo p: return $"P:{FullName(p.DeclaringType)}.{p.Name}";
        case MethodInfo m: return $"M:{FullName(m.DeclaringType)}.{m.Name}";
        default:
          throw new Exception($"Invalid object type for xml doc lookup: {obj}"); 
      }
    }

    private string FullName(Type t) {
      return t.Namespace + "." + t.Name;
    }

    private bool TryLoadAssemblyXmlFile(Assembly assembly) {
      var asmName = assembly.GetName().Name;
      if (_data.TryGetValue(asmName, out var dict))
        return dict != null;
      _data[asmName] = null; // set null value to mark assembly as checked for xml file

      var asmLoc = assembly.Location;
      var dir = Path.GetDirectoryName(asmLoc);
      var fileName = Path.GetFileNameWithoutExtension(asmLoc);
      var filePath = Path.Combine(dir, fileName + ".xml");
      if (!File.Exists(filePath))
        return false;

      //Load all member elements
      var xml = File.ReadAllText(filePath);
      var xDoc = XDocument.Parse(xml);
      var memberNodes = xDoc.Root.Descendants("members").First().Descendants("member").ToList();

      // Add a dictionary with all member elements for the assembly
      dict = new Dictionary<string, XElement>();
      _data[asmName] = dict;
      foreach (var mElem in memberNodes) {
        var key = mElem.Attribute("name").Value;
        //for methods, cut off param list in parenthesis - we do not support overloading anyway
        //  (param types are needed to resolve overloads)
        var pIndex = key.IndexOf('(');
        if (pIndex >= 0)
          key = key.Substring(0, pIndex);
        dict[key] = mElem;
      }
      return true;
    }

  }
}
