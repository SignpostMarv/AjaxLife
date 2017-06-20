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
using HttpServer;
using OpenMetaverse.StructuredData;
using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace AjaxLife.HttpRules.API
{
    class LoginDetails : AbstractHttpPostRule
    {
        private Dictionary<Guid, User> users;

        public LoginDetails(Dictionary<Guid, User> users) : base("sessiondetails")
        {
            this.users = users;
        }

        public override OSD HandleRequest(IHttpRequest request)
        {
            OSDMap data = new OSDMap();

            try
            {
                GridClient client;
                User user;
                // Get the session.
                HttpForm POST = request.Form;
                Guid sessionid = new Guid(POST["sid"].Value);
                // Standard user stuff.
                lock (users)
                {
                    user = users[sessionid];
                    client = user.Client;
                    user.LastRequest = DateTime.Now;
                    // Clear the inventory cache to avoid confusion.
                    user.Events.ClearInventory();
                }
                OSDMap pos = new OSDMap();
                pos.Add("x", (int)Math.Floor(client.Self.GlobalPosition.X / 256));
                pos.Add("y", (int)Math.Floor(client.Self.GlobalPosition.Y / 256));
                data.Add("RegionCoords", pos);
                data.Add("Region", client.Network.CurrentSim.Name);
                data.Add("Position", client.Self.SimPosition);
                data.Add("MOTD", client.Network.LoginMessage);
                data.Add("UserName", client.Self.FirstName + " " + client.Self.LastName);
                data.Add("AgentID", client.Self.AgentID);
                data.Add("InventoryRoot", client.Inventory.Store.RootFolder.UUID);
            }
            catch (Exception exception)
            {
                data.Clear();
                data.Add("success", false);
                data.Add("exception", exception.Message);
            }

            return data;
        }
    }
}
