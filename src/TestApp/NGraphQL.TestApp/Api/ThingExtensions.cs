using System;
using System.Collections.Generic;
using System.Text;

namespace NGraphQL.TestApp {
  public static class ThingExtensions {

    // testing bug fix
    public static OtherThingWrapper_ GetWrapper(this OtherThing otherTh) {
      if (otherTh == null)
        return null; 
      return new OtherThingWrapper_() { OtherThingName = otherTh.Name, WrappedOn = DateTime.Now, 
        OtherThing = new OtherThing_() { IdStr = otherTh.IdStr, Name = otherTh.Name}  
      };
    }

  }
}
