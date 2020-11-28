using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace NGraphQL {

  public static class Util {

    // not used, and probably should NOT be used
    public static void Check(bool cond, string template, params object[] args) {
      if(cond)
        return;
      var message = SafeFormat(template, args);
      throw new Exception(message); 
    }

    public static string SafeFormat(string template, params object[] args) {
      try {
        return string.Format(CultureInfo.InvariantCulture, template, args);
      } catch(Exception ex) {
        var strArgs = string.Join(", ", args);
        return template + $" (System error: failed to format message; error {ex.Message}; formatting args: {strArgs})";
      }
    }

    public static string FirstLower(this string name) {
      return char.ToLower(name[0]) + name.Substring(1);
    }

    public static string ToUnderscoreCase(this string value) {
      if(string.IsNullOrEmpty(value))
        return value;
      var chars = value.ToCharArray();
      char prevCh = '\0';
      var newChars = new List<char>();
      foreach(var ch in chars) {
        if(char.IsUpper(ch)) {
          if(newChars.Count > 0 && prevCh != '_' && !char.IsUpper(prevCh)) //avoid double-underscores
            newChars.Add('_');
          newChars.Add(ch);
        } else
          newChars.Add(ch);
        prevCh = ch;
      }
      var result = new string(newChars.ToArray()).Replace("__", "_"); //cleanup double _, just in case
      return result;
    }

    public static bool HasAny<T>(this IList<T> list) {
      // all of cases it is lists
      return list != null && list.Count > 0;
    }

    public static IList<T> MergeLists<T>(this IList<T> list, IList<T> other) {
      if(list == null || list.Count == 0)
        return other;
      if(other == null || other.Count == 0)
        return list;
      var newList = new List<T>(list);
      newList.AddRange(other);
      return newList; 
    }

    public static string ToText(this Exception ex) {
      var exStr = ex.ToString();
      if(ex.Data.Count == 0)
        return exStr;
      var sList = new List<string>(); 
      foreach(var key in ex.Data.Keys) 
        sList.Add($"{key} = {ex.Data[key]}");
      var sData = string.Join(Environment.NewLine, sList);
      return exStr + Environment.NewLine + sData; 
        
    }



  } //class
}
