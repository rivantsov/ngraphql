namespace NGraphQL.CodeFirst {

  public interface IResolverClass {
    void BeginRequest(IRequestContext request);
    void EndRequest(IRequestContext request);
  }

}
