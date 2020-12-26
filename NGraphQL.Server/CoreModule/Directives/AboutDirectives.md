## About directives
Classes: 
* DirectiveAttribute - base attribute for attributes defining directives - both for type system and executables
* DirectiveDef - internal model object, meta info about directive, arg defs, directive handler  
* RequestDirectiveRef - parsed from request, references DirectiveDef, with arg refs (maybe with vars)
* DirectiveHandler - registered handler type for a directive type; runtime directive instances from request dir ref; 

Directives on type system objects - defined by attrs, type defs contain directive handlers, ready to be applied
Request: 
   elements contain list of handlers for those dirs that do NOT depend on variables in args; 
   for directives with vars in args, it contains RequestDirectiveRef list; runtime dir is produced at exec time, when args are evaluated
At execution time, var-dependent dirs and final list of runtime dirs: 
  Arg values cannot have directives (only arg defs - InputValue defs), so handlers are ready to use
  Selection fields - final list of dirs is put into FieldContext
  Fragm spread, inline fragments - in mapped fragment (to be defined) 

## Directive scopes: 
* Type system directives - declared on schema elements using corresponding attributes
  Each type system model element contains a list of handlers with evaluated arguments;
    each directive references: 
      - DirectiveDef (internal model dir definition)
      - Directive action set (retrieved from directive handler for a given Directive)

* Executable directive locations 
  * selection items (fields, fragment spreads, inline fragments)
  * var defs
  * fragment defs

  Directive handlers sources: 
     args, input fields - from InputValueDefs 
     vars - from var defs in query (InputValueDefs)
     fields, fragment spreads
         for all query elements: directives on return types, on container types or parent fields
     dependency on variables - possible only for fields, fragments and fragment spreads