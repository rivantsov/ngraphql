## About directives
Classes: 
* DirectiveAttribute - base attribute for attributes defining directives - both for type system and executables
* DirectiveDef - internal model object, meta info about directive, arg defs, directive handler  
* RequestDirectiveRef - parsed from request, references DirectiveDef, with arg refs (maybe with vars)
* DirectiveHandler - registered handler type for a directive type; runtime directive instances from request dir ref; 

Type system objects contain lists of directive handlers, ready to be appied
Request elements contain list of handlers for those dirs that do NOT depend on variables in args; 
   for directives with vars in args, it contains RequestDirectiveRef list; runtime dir is produced at exec time
At execution time, var-dependent dirs and final list of runtime dirs: 
  Arg values cannot have directives (only arg defs - InputValue defs), so handlers are ready to use
  Selection fields - final list of dirs is put into FieldContext
  Fragm spread, inline fragments - in mapped fragment (to be defined) 

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