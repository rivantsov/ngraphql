
using System.Threading;
using System.Threading.Tasks;

namespace Things.GraphQL.HttpServer {

  public class Program
  {
    public static void Main(string[] args) {
      var task = ThingsWebServerStartupHelper.StartThingsGraphqQLWebServer(args, useGraphiql: true, enablePreviewFeatures: false);
      task.Wait(); 
    }
  }
}
