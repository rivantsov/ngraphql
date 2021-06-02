using System;
using System.Collections.Generic;
using System.Text;
using NGraphQL.CodeFirst;

namespace Things.GraphQL.Types {
  public interface IThingsMutation {
    Thing_ MutateThing(int id, string newName);
    Thing_ MutateThingWithValidation(int id, string newName);
  }
}
