using System;
using System.Collections.Generic;
using System.Text;

namespace StarWars {
  public static class StarWarsExtensions {
    public static bool Includes(this Episode episodes, Episode episode) {
      return (episodes & episode) != 0;
    }

    public static float? MetricToFeet(this float? metricValue) {
      if (metricValue == null)
        return null;
      return metricValue.Value * 3.28f;
    }

    public static int GetIndexOf<T>(this IList<T> list, Func<T, bool> func) {
      for(int i = 0; i < list.Count; i++) 
        if (func(list[i]))
          return i;
      return -1;
    } 

  }
}
