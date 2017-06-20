#region License
/* Copyright (c) 2008, Katharine Berry
 * Copyright (c) 2017, SignpostMarv
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of Katharine Berry nor the names of any contributors
 *       may be used to endorse or promote products derived from this software
 *       without specific prior written permission.
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using HttpServer;
using OpenMetaverse.StructuredData;
using OpenMetaverse;

namespace AjaxLife.HttpRules.API
{
    class Logout : AbstractHttpPostRuleOSD
    {
        private Dictionary<Guid, User> users;

        public Logout(Dictionary<Guid, User> users) : base("logout")
        {
            this.users = users;
        }

        public override OSD HandleRequest(IHttpRequest request)
        {
            OSD data = new OSDMap();

            try
            {
                // Get the user session and GridClient object.
                HttpForm POST = request.Form;
                Guid session = new Guid(POST["sid"].Value);
                GridClient client;
                User user;
                lock (users)
                {
                    user = users[session];
                    client = user.Client;
                    user.LastRequest = DateTime.Now;
                }
                // If we're connected, request a logout.
                if (client.Network.Connected)
                {
                    client.Network.Logout();
                    client.Network.Shutdown(NetworkManager.DisconnectType.ClientInitiated);
                    System.Threading.Thread.Sleep(2000);
                }
                // Deactivate the event queue.
                if (user.Events != null)
                {
                    user.Events.deactivate();
                }
                // Unset everything for garbage collection purposes.
                user.Events = null;
                user.Client = null;
                client = null;
                user.Avatars = null;
                // Remove the user
                lock (users)
                {
                    users.Remove(session);
                }
                
                // Announce our success.
                ((OSDMap)data).Clear();
                ((OSDMap)data).Add("success", true);
            }
            catch (Exception exception)
            {
                ((OSDMap)data).Clear();
                ((OSDMap)data).Add("success", false);
                ((OSDMap)data).Add("exception", exception.Message);
            }

            return data;
        }
    }
}
