
using System.Threading;
using System.Threading.Tasks;

namespace Things.GraphQL.HttpServer {

  public class Program
  {
    public static void Main(string[] args) {
      var task = TestServerStartup.SetupServer(args, useGraphiql: true, enablePreviewFeatures: false);
      task.Wait(); 
    }
  }
}
