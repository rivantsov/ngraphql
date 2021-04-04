using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NGraphQL.CodeFirst;
using NGraphQL.Introspection;

namespace NGraphQL.Model {

  public static class DirectiveRegistrationExtensions {

    public static void RegisterDirective(this GraphQLModule module, string name, string signatureMethodName,
           DirectiveLocation locations, string description = null, IDirectiveHandler handler = null, bool listInSchema = true) {
      var method = module.GetType().GetMethod(signatureMethodName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
      if (method == null)
        throw new ArgumentException($"RegisterDirective, name={name}: method {signatureMethodName} not found in module {module.Name}");
      RegisterDirective(module, name, method, locations, description, handler, listInSchema);
    }

    public static void RegisterDirective(this GraphQLModule module, string name, MethodInfo signature,
           DirectiveLocation locations, string description = null, IDirectiveHandler handler = null, bool listInSchema = true) {
      if (signature == null)
        throw new ArgumentException("RegisterDirective method: signature parameter may not be null.");
      var reg = new DirectiveRegistration() {
        Name = name, Signature = signature, Locations = locations, Description = description,
        ListInSchema = listInSchema, Handler = handler
      };
      module.RegisteredDirectives.Add(reg);
    }

    public static void RegisterDirective(this GraphQLModule module, string name, Type directiveAttributeType,
           DirectiveLocation locations, string description = null, IDirectiveHandler handler = null, bool listInSchema = true) {
      if (!typeof(BaseDirectiveAttribute).IsAssignableFrom(directiveAttributeType))
        throw new ArgumentException(
          $"RegisterDirective method: directive attribute must be subclass of {nameof(directiveAttributeType)}");
      var reg = new DirectiveRegistration() {
        Name = name, AttributeType = directiveAttributeType, Locations = locations, Description = description,
        ListInSchema = listInSchema, Handler = handler
      };
      module.RegisteredDirectives.Add(reg);
    }

    /// <summary>Registers a handler for a directive that is already registered, possibly in another module. </summary>
    /// <param name="module">GraphQL module.</param>
    /// <param name="name">Directive name.</param>
    /// <param name="handler">Handler instance.</param>
    /// <remarks>The purpose of this method is to allow registering handlers separately from the directives themselves.
    /// You can have a directive defined in light, module not dependent on any server-side assemblies, so it can be 
    /// used in client-side code. The directive's handler can be registered in server-side assembly, as it likely 
    /// needs access to server-side functionality. 
    /// </remarks>
    public static void RegisterDirectiveHandler(this GraphQLModule module, string name, IDirectiveHandler handler = null) {
      var reg = new DirectiveRegistration() {
        Name = name, Handler = handler
      };
      module.RegisteredDirectives.Add(reg);
    }


  }
}
