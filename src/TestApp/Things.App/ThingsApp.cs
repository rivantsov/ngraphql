using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Things {

  /****************************************************************************************
   A sample 'business' app behind the sample GraphQLApi, managing 'Things'
  ****************************************************************************************/


  public partial class ThingsApp {
    public static ThingsApp Instance;

    // it is just things and other-things
    public List<Thing> Things;
    public List<OtherThing> OtherThings; 

    public ThingsApp() {
      Instance = this; 
      TestDataGenerator.CreateTestData(this); 
    }
  
  } //class
}
