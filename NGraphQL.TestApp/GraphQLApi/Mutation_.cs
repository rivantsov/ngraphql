using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {
  [Mutation]
  public class Mutation_ {
    public BizThing MutateThing(int id, string newName) {
      return default; 
    }

    public BizThing MutateThingWithValidation(int id, string newName) {
      return default; 
    }

  }
}
