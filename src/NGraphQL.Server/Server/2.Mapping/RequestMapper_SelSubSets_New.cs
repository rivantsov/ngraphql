using System.Collections.Generic;
using System.Linq;
using NGraphQL.Introspection;
using NGraphQL.Model;
using NGraphQL.Model.Request;

namespace NGraphQL.Server.Mapping {

  /// <summary>RequestMapper takes request tree and maps its objects to API model; for ex: selection field is mapped to field definition</summary>
  public partial class RequestMapper {

    private void MapOperation2(GraphQLOperation op) {
      _currentOp = op; 
      //MapSelectionSubSet2(op.OperationTypeDef, op.SelectionSubset);
    }

    private void CollectAllFields (MappedSelectionSubSet main, MappedSelectionSubSet other) {
       
    }

  } // class
}
