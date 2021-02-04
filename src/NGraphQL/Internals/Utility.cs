using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.Model {

  internal static class Utility {
    
    public static string ToUnderscoreUpperCase(string value) {
      if (string.IsNullOrEmpty(value))
        return value;
      var chars = value.ToCharArray();
      char prevCh = '\0';
      var newChars = new List<char>();
      foreach (var ch in chars) {
        if (char.IsUpper(ch)) {
          if (newChars.Count > 0 && prevCh != '_' && !char.IsUpper(prevCh)) //avoid double-underscores
            newChars.Add('_');
          newChars.Add(ch);
        } else
          newChars.Add(ch);
        prevCh = ch;
      }
      var result = new string(newChars.ToArray()).Replace("__", "_"); //cleanup double _, just in case
      result = result.ToUpperInvariant();
      return result;
    }

  }
}
