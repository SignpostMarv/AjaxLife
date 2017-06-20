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
using System.Linq;

namespace AjaxLife.HttpRules.API
{
    public class CreateSession : AbstractRule
    {
        private Dictionary<Guid, User> users;

        public CreateSession(Dictionary<Guid, User> users) : base("newsession")
        {
            this.users = users;
        }

        public override string[] Methods
        {
            get
            {
                return new string[]
                {
                    "POST"
                };
            }
        }

        public override OSD HandleRequest(IHttpRequest request)
        {
            OSDMap ret = new OSDMap();

            Guid key = Guid.NewGuid();
            User user = User.CreateUser();
            lock (users) users.Add(key, user);

            Dictionary<string, string>.KeyCollection GridKeys = (
                AjaxLife.LOGIN_SERVERS.Keys
            );
            OSDArray GridsRet = new OSDArray();
            string[] Grids = AjaxLife.LOGIN_SERVERS.Keys.ToArray<string>();
            int i, j;
            j = Grids.Length;
            for (i=0;i<j;++i)
            {
                GridsRet.Add(Grids[i]);
            }

            ret.Add("SessionID", key.ToString("D"));
            ret.Add("Challenge", user.Challenge);
            ret.Add("Exponent", StringHelper.BytesToHexString(AjaxLife.RSAp.Exponent));
            ret.Add("Modulus", StringHelper.BytesToHexString(AjaxLife.RSAp.Modulus));
            ret.Add("Grids", GridsRet);
            ret.Add("DefaultGrid", AjaxLife.DEFAULT_LOGIN_SERVER);

            return ret;
        }
    }
}
