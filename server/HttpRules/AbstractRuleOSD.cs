using AjaxLife.Http;
using HttpServer;
using HttpServer.Rules;
using OpenMetaverse.StructuredData;
using System.IO;
using System.Linq;
using System.Net;

namespace AjaxLife.HttpRules
{
    public abstract class AbstractRuleOSD : IRule, ICanHandleRequest
    {
        private string apiPath { get; set;}

        public abstract string[] Methods { get; }

        public AbstractRuleOSD(string path)
        {
            if (path.StartsWith("/") == false)
            {
                path = "/" + path;
            }
            if (path.StartsWith("/api/") == false)
            {
                path = "/api" + path;
            }
            apiPath = path;
        }

        public virtual bool CanHandleRequest(IHttpRequest request)
        {
            return request.UriPath == apiPath;
        }

        public abstract OSD HandleRequest(IHttpRequest request);

        public bool Process(IHttpRequest request, IHttpResponse response)
        {
            if (CanHandleRequest(request) == false)
            {
                return false;
            }
            else if (Methods.Contains(request.Method) == false)
            {
                response.Status = HttpStatusCode.MethodNotAllowed;
                return true;
            }

            OSD res = HandleRequest(request);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(OSDParser.SerializeJsonString(res));
            writer.Flush();

            stream.Position = 0;

            response.Body = stream;

            return true;
        }
    }
}
