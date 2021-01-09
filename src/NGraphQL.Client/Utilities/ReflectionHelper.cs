using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NGraphQL.Client.Utilities {

  internal static class ReflectionHelper {

    public static bool CheckNullable(ref Type type) {
      if (!type.IsValueType)
        return true;
      var underType = Nullable.GetUnderlyingType(type);
      if (underType != null) {
        type = underType;
        return true;
      }
      return false;
    }

    public static bool HasAttribute<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      return provider.GetAttribute<TAttr>() != null;
    }

    public static TAttr GetAttribute<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      var attr = provider.GetAttributes<TAttr>().FirstOrDefault();
      return attr;
    }

    public static IList<TAttr> GetAttributes<TAttr>(this ICustomAttributeProvider provider) where TAttr : Attribute {
      var attrs = provider.GetCustomAttributes(inherit: true).Where(a => a is TAttr).OfType<TAttr>().ToList();
      return attrs;
    }

  }
}
