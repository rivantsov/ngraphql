using System.Threading.Tasks;

namespace NGraphQL.CodeFirst {

  public interface IResolverClass {
    void BeginRequest(IRequestContext request);
    void EndRequest(IRequestContext request);
  }

  public interface IResolverClassAsync {
    Task BeginRequestAsync(IRequestContext request);
    Task EndRequestAsync(IRequestContext request);
  }

}
