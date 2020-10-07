using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace NGraphQL.TestApp {
  [Mutation]
  public interface IThingsMutation {
    Thing MutateThing(int id, string newName);
    Thing MutateThingWithValidation(int id, string newName);
  }
}
