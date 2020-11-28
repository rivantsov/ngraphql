using System;

namespace NGraphQL.Client {

  public static class ClientExtensions {

    public static void EnsureNoErrors(this ServerResponse response) {
      if (response.Errors == null || response.Errors.Count == 0)
        return;
      var errText = response.GetErrorsAsText();
      var msg = "Request failed.";
      if (!string.IsNullOrWhiteSpace(errText))
        msg += " Error(s):" + Environment.NewLine + errText;
      throw new Exception(msg);
    }

    public static string GetErrorsAsText(this ServerResponse response) {
      if (response.Errors == null || response.Errors.Count == 0)
        return string.Empty;
      var text = string.Join(Environment.NewLine, response.Errors);
      return text;
    }

  }
}
