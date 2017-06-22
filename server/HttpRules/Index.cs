using AjaxLife.Http.Rules;
using HttpServer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace AjaxLife.HttpRules
{
    class Index : DynamicStringRule
    {
        private Dictionary<Guid, User> users;

        private static List<string> DefaultPathsToFileOnServer = new List<string>
        {
                "/",
                "/index.html"
        };

        public Index(
            Dictionary<Guid, User> users,
            List<string> pathToFileOnServer=null,
            string contentType = "text/html; charset=utf-8"
        ) : base(
            pathToFileOnServer == null ? DefaultPathsToFileOnServer : pathToFileOnServer,
            contentType
        )
        {
            this.users = users;
        }

        protected override bool HandleRequest(IHttpRequest request, IHttpResponse response)
        {
            response.Body = new MemoryStream();
            response.ContentType = "text/html; charset=utf8";
            StreamWriter writer = new StreamWriter(response.Body);
            try
            {
                // Generate a new session ID.
                Guid key = Guid.NewGuid();
                // Create a new User.
                User user = new User();
                // Set the user session properties.
                user.LastRequest = DateTime.Now;
                user.Rotation = -Math.PI;
                // Generate a single-use challenge key.
                System.Security.Cryptography.RSAParameters foo = AjaxLife.RSAp;
                string challenge = RSACrypto.CreateChallengeString(foo);
                user.Challenge = challenge;
                // Add the session to the users.
                lock (users) users.Add(key, user);
                Hashtable hash = new Hashtable();
                // Set up the template with useful details and the challenge and public key.
                hash.Add("STATIC_ROOT", AjaxLife.STATIC_ROOT);
                hash.Add("API_ROOT", AjaxLife.API_ROOT);
                hash.Add("SESSION_ID", key.ToString("D"));
                hash.Add("CHALLENGE", user.Challenge);
                hash.Add("RSA_EXPONENT", StringHelper.BytesToHexString(AjaxLife.RSAp.Exponent));
                hash.Add("RSA_MODULUS", StringHelper.BytesToHexString(AjaxLife.RSAp.Modulus));
                // Make the grid list, ensuring the default one is selected.
                string grids = "";
                foreach (string server in AjaxLife.LOGIN_SERVERS.Keys)
                {
                    grids += "<option value=\"" + System.Web.HttpUtility.HtmlAttributeEncode(server) +
                        "\"" + (server == AjaxLife.DEFAULT_LOGIN_SERVER ? " selected=\"selected\"" : "") + ">" +
                        System.Web.HttpUtility.HtmlEncode(server) + "</option>\n";
                }
                hash.Add("GRID_OPTIONS", grids);
                if (AjaxLife.HANDLE_CONTENT_ENCODING)
                {
                    hash.Add("ENCODING", "identity");
                    // S3 doesn't support Accept-Encoding, so we do it ourselves.
                    if (request.Headers["Accept-Encoding"] != null)
                    {
                        string[] accept = request.Headers["Accept-Encoding"].Split(',');
                        foreach (string encoding in accept)
                        {
                            string parsedencoding = encoding.Split(';')[0].Trim();
                            if (parsedencoding == "gzip" || parsedencoding == "*") // Should we really honour "*"? Specs aside, it's never going to be true.
                            {
                                hash["ENCODING"] = "gzip";
                                break;
                            }
                        }
                    }
                }
                // Parse the template.
                Html.Template.Parser parser = new Html.Template.Parser(hash);
                writer.Write(parser.Parse(File.ReadAllText("client/Templates/index.html")));
            }
            catch (Exception exception)
            {
                response.ContentType = "text/plain";
                writer.WriteLine("Error: " + exception.Message);
            }
            writer.Flush();
            return true;
        }
    }
}
