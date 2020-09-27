using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {
  // Query, Mutation and Subscription types can be either class or interface; we use class here
  [Mutation]
  public class ThingsMutation {
    public Thing MutateThing(int id, string newName) {
      return default; 
    }

    public Thing MutateThingWithValidation(int id, string newName) {
      return default; 
    }

  }
}
