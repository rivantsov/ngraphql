## About directives
Classes: 
* DirectiveAttribute - base attribute for attributes defining directives - both for type system and executabl
* DirectiveHandler - runtime handler for a directive (runtime instace); 
* DirectiveDef - internal model object, meta info about directive, including directive handler 
* Directive (RuntimeDirective) - instance of directive with assigned arg values

### Directive scopes: 
* Type system directives - declared on schema elements using corresponding attributes
  Each type system model elemebt contains a list of RuntimeDirectives - instances with evaluated arguments;
    each directive references: 
      - DirectiveDef (internal model dir definition)
      - Directive action set (retrieved from directive handler for a given Directive)

* Executable directive locations 
  * selection items (fields, fragment spreads, inline fragments)
  * var defs
  * fragment defs

  Effective runtime directives: 
     args, input fields - from InputValueDefs 
     vars - from var defs in query, from InputValueDefs
     fields, fragment spreads
         for all query elements: directives on return types, on container types or parent fields
     dependency on variables - possible only for fields, fragments and fragment spreads