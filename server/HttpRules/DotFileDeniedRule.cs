using HttpServer;
using HttpServer.Rules;
using System.Linq;

namespace AjaxLife.HttpRules
{
    public class DotFileDeniedRule : IRule
    {
        public bool Process(IHttpRequest request, IHttpResponse response)
        {
            if(request.UriParts.Any(s => s.StartsWith(".")))
            {
                response.Status = System.Net.HttpStatusCode.Forbidden;
                return true;
            }
            return false;
        }
    }
}
