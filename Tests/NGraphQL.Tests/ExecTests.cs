﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NGraphQL.Runtime;
using NGraphQL.Server;

namespace NGraphQL.Tests {
  [TestClass]
  public partial class ExecTests {

    [TestInitialize]
    public void Init() {
      TestEnv.Init();
    }

    [TestCleanup]
    public void TestCleanup() {
    }

    public Task<GraphQLResponse> ExecuteAsync(string query, IDictionary<string, object> vars = null, bool throwOnError = true) {
      return TestEnv.ExecuteAsync(query, vars, throwOnError);
    }
  }
}
