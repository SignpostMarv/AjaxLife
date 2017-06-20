#region License
/* Copyright (c) 2008, Katharine Berry
 * Copyright (c) 2017, SignpostMarv
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *       * Redistributions of source code must retain the above copyright
 *         notice, this list of conditions and the following disclaimer.
 *       * Redistributions in binary form must reproduce the above copyright
 *         notice, this list of conditions and the following disclaimer in the
 *         documentation and/or other materials provided with the distribution.
 *       * Neither the name of Katharine Berry nor the names of any contributors
 *         may be used to endorse or promote products derived from this software
 *         without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY KATHARINE BERRY ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL KATHARINE BERRY BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 ******************************************************************************/
#endregion
using AjaxLife.Http;
using HttpServer;
using HttpServer.Rules;
using OpenMetaverse.StructuredData;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using OpenMetaverse;
using Newtonsoft.Json;

namespace AjaxLife.HttpRules.API
{
    class EventQueue : IRule, ICanHandleRequest
    {
        private Dictionary<Guid, User> users;

        private string apiPath;

        public string[] Methods
        {
            get
            {
                return new string[]
                {
                    "POST"
                };
            }
        }

        public EventQueue(Dictionary<Guid, User> users, string path = "/api/events")
        {
            this.users = users;
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
            response.Body = new MemoryStream();
            StreamWriter writer = new StreamWriter(response.Body);

            try
            {
                // Decode the POST data.
                HttpForm POST = request.Form;
                Guid session = new Guid(POST["sid"].Value);
                Events eventqueue;
                User user;
                GridClient client;
                // Load in the session data.
                lock (users)
                {
                    user = users[session];
                    eventqueue = user.Events;
                    client = user.Client;
                    user.LastRequest = DateTime.Now;
                }
                bool sent = false;
                double heading = user.Rotation;
                // Check once per second, timing out after 15 seconds.
                for (int i = 0; i < 15; ++i)
                {
                    // Ugly hack - we're riding on the back of the event poll to rotate our camera.
                    if (user.Rotation != -4)
                    {
                        // If we've reached π, having started at -π, we're done. Quit rotating, because it
                        // appears to annoy people and/or make them dizzy.
                        heading += 0.5d;
                        if (heading > Math.PI)
                        {
                            // We use -4 because -4 < -π, so will never occur during normal operation.
                            user.Rotation = -4;
                            heading = Math.PI;
                            // Reset the draw distance to attempt to reduce traffic. Also limits the
                            // nearby list to people within chat range.
                            user.Client.Self.Movement.Camera.Far = 20.0f;
                            user.Client.Self.Movement.SendUpdate();
                        }
                        else
                        {
                            user.Rotation = heading;
                        }
                        client.Self.Movement.UpdateFromHeading(heading, false);
                    }

                    if (eventqueue.GetEventCount() > 0)
                    {
                        writer.WriteLine(eventqueue.GetPendingJson(client));
                        sent = true;
                        break;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                // If nothing of interest ever came up, we just send the standard footer.
                if (!sent)
                {
                    JsonWriter w = new JsonWriter(writer);
                    w.WriteStartArray();
                    (new JsonSerializer()).Serialize(w, eventqueue.GetFooter(client));
                    w.WriteEndArray();
                    w.Flush();
                }
            }
            catch (Exception exception)
            {
                writer.WriteLine(exception.Message);
            }

            writer.Flush();

            return true;
        }
    }
}
