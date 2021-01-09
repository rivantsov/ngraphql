using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Utilities;

namespace NGraphQL.Tests {
  partial class ExecTests {

    [TestMethod]
    public async Task Test_Introspection_Misc() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@" misc introspection queries.");
      var introQuery = @"
query {
  ThingType: __type(name: ""Thing"") {
     name 
     fields { 
       name, 
       type { displayName }           # displayName is our extension to spec 
     }
  }
  typeKind: __type(name: ""__TypeKind"") {
     name enumValues  (includeDeprecated: true) {name}
  }
} ";
      var resp = await ExecuteAsync(introQuery); // just check it goes ok

      TestEnv.LogTestDescr(@" introspection queries, checking isDeprecated and and deprecationReason fields.");
      introQuery = @"
query introQuery {
  inputObjType: __type (name: ""InputObj"") {
    name
    inputFields {
      name
      isDeprecated
      deprecationReason
    }
  }
}
";
      resp = await ExecuteAsync(introQuery);
      var inpObj = resp.Data["inputObjType"];
      Assert.IsNotNull(inpObj, "Expected input obj type");

      TestEnv.LogTestDescr(@" Introspection, querying all __schema fields");
      introQuery = @"
query introQuery {
  __schema {
      queryType         { name fields { name } }
      mutationType      { name fields { name } }
      subscriptionType  { name fields { name } }
      types  { name }
      directives { name }
  }
}
";
      resp = await ExecuteAsync(introQuery);
      var typeList = resp.GetValue<IList>("__schema.types");
      Assert.IsTrue(typeList.Count > 5, "Expected types");

    } //method

    [TestMethod]
    public async Task Test_Introspection_GraphiqlQuery() {
      TestEnv.LogTestMethodStart();

      TestEnv.LogTestDescr(@" testing Graphiql introspection query.");
      var query = _graphiqlIntroQuery;
      var resp = await ExecuteAsync(query);
      Assert.AreEqual(0, resp.Errors.Count);
    }

    // This query is fired by Graphiql UI tool at startup to request schema data
    string _graphiqlIntroQuery = @"
    query IntrospectionQuery {
      __schema {
        
        queryType { name }
        mutationType { name }
        subscriptionType { name }
        types {
          ...FullType
        }
        directives {
          name
          description
          
          locations
          args {
            ...InputValue
          }
        }
      }
    }

    fragment FullType on __Type {
      kind
      name
      description
      
      fields(includeDeprecated: true) {
        name
        description
        args {
          ...InputValue
        }
        type {
          ...TypeRef
        }
        isDeprecated
        deprecationReason
      }
      inputFields {
        ...InputValue
      }
      interfaces {
        ...TypeRef
      }
      enumValues(includeDeprecated: true) {
        name
        description
        isDeprecated
        deprecationReason
      }
      possibleTypes {
        ...TypeRef
      }
    }

    fragment InputValue on __InputValue {
      name
      description
      type { ...TypeRef }
      defaultValue
    }

    fragment TypeRef on __Type {
      kind
      name
      ofType {
        kind
        name
        ofType {
          kind
          name
          ofType {
            kind
            name
            ofType {
              kind
              name
              ofType {
                kind
                name
                ofType {
                  kind
                  name
                  ofType {
                    kind
                    name
                  }
                }
              }
            }
          }
        }
      }
    }
        
      ";
  }
}
