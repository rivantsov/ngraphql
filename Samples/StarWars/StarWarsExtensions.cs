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
  }
}
