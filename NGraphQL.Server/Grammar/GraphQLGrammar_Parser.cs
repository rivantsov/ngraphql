using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;

namespace NGraphQL.Grammar {

  // Methods for creating parsers 
  partial class GraphQLGrammar {
    LanguageData _languageData;

    public void Init() {
      _languageData = new LanguageData(this);
    }

    public Parser CreateRequestParser() {
      if(_languageData == null)
        Init(); 
      return new Parser(_languageData);
    }

    public Parser CreateSchemaParser() {
      if(_languageData == null)
        Init();
      return new Parser(_languageData, this.SchemaDocRoot);
    }

  }
}
