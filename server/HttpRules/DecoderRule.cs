using HttpServer.Rules;
using HttpServer.FormDecoders;
using HttpServer;

namespace AjaxLife.HttpRules
{
    class DecoderRule : IRule
    {
        private FormDecoderProvider _formDecodersProvider;

        public DecoderRule()
        {
            _formDecodersProvider = new FormDecoderProvider();
            _formDecodersProvider.Add(new UrlDecoder());
            _formDecodersProvider.Add(new MultipartDecoder());
            _formDecodersProvider.Add(new XmlDecoder());
        }

        public bool Process(IHttpRequest request, IHttpResponse response)
        {
            request.DecodeBody(_formDecodersProvider);
            return false;
        }
    }
}
