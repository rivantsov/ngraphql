using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using NGraphQL.Client.Types;

namespace NGraphQL.Client {

  public class GraphQLResult {
    public readonly ClientRequest Request;
    public DeserializedGraphQLResponse ResponseBody;
    public IList<GraphQLError> Errors => ResponseBody?.Errors;
    public string ResponseJson;
    public double DurationMs;
    public Exception Exception;

    private JsonSerializerOptions _jsonOptions;

    public GraphQLResult(ClientRequest request, JsonSerializerOptions jsonOptions) {
      Request = request;
      _jsonOptions = jsonOptions;
    }

    public bool HasErrors() => Errors != null && Errors.Count > 0;
    public bool HasData() {
      var elem = ResponseBody?.Data;
      if (elem == null)
        return false;
      var valueKind = elem.Value.ValueKind;
      if (valueKind == JsonValueKind.Null || valueKind == JsonValueKind.Undefined)
        return false;
      return true;
    }
    public string GetDataJson() {
      if (!HasData())
        return null;
      return RootDataElem.GetRawText();
    }

    public IDictionary<string, JsonElement> TopFields {
      get {
        if (_topFields == null) {
          if (HasData())
            _topFields = RootDataElem.Deserialize<Dictionary<string, JsonElement>>(_jsonOptions);
          else
            _topFields = _emptyDict;
        }
        return _topFields;
      }
    } IDictionary<string, JsonElement> _topFields;
    private static IDictionary<string, JsonElement> _emptyDict = new Dictionary<string, JsonElement>();


    public T GetTopField<T>(string name) {
      if (!TopFields.TryGetValue(name, out var jsonElem))
        throw new Exception($"Field '{name}' not found in response data.");
      switch (jsonElem.ValueKind) {
        case JsonValueKind.Null:
        case JsonValueKind.Undefined:
          if (IsNullable(typeof(T)))
            return (T)(object)null;
          else
            throw new Exception($"Field '{name}': cannot convert null value to type {typeof(T)}.");
        default:
          var result = jsonElem.Deserialize<T>(_jsonOptions);
          return result;
      }
    }

    public void EnsureNoErrors() {
      if (!HasErrors())
        return;
      var errText = GetErrorsAsText();
      var msg = "Request failed.";
      if (!string.IsNullOrWhiteSpace(errText))
        msg += " Error(s):" + Environment.NewLine + errText;
      throw new Exception(msg);
    }

    public string GetErrorsAsText() {
      if (!HasErrors())
        return string.Empty;
      var text = string.Join(Environment.NewLine, Errors);
      return text;
    }

    private JsonElement RootDataElem => ResponseBody.Data;

    public static bool IsNullable(Type type) {
      if (!type.IsValueType)
        return true;
      return Nullable.GetUnderlyingType(type) != null;
    }

  } //class
}
