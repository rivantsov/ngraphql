using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace NGraphQL.Utilities {

  internal static class Utility {

    public static void Check(bool condition, string message, params object[] args) {
      if (!condition)
        Throw(message, args);
    }
    public static Exception Throw(string message, params object[] args) {
      var msg = SafeFormat(message, args);
      throw new Exception(msg);
    }

    public static string SafeFormat(string message, params object[] args) {
      if (args == null || args.Length == 0)
        return message;
      try {
        return string.Format(CultureInfo.InvariantCulture, message, args);
      } catch (Exception ex) {
        return message + " (System error: failed to format message. " + ex.Message + ")";
      }
    }

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
