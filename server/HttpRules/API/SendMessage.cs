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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Net;
using MiniHttpd;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Packets;
using Newtonsoft.Json;
using Affirma.ThreeSharp;
using Affirma.ThreeSharp.Query;

namespace AjaxLife.HttpRules.API
{
    class SendMessage : AbstractHttpPostRule
    {
        private Dictionary<Guid, User> users;
        private MD5 md5 = MD5CryptoServiceProvider.Create();
        // Anything in this list must be signed to be accepted. 
        // Should match (or be a subset of) the list in client/AjaxLife.Network.js.
        private string[] REQUIRED_SIGNATURES = {
            "AcceptFriendship",
            "DeclineFriendship",
            "OfferFriendship",
            "TerminateFriendship",
            "SendAgentMoney",
            "EmptyTrash",
            "MoveItem",
            "MoveFolder",
            "MoveItems",
            "MoveFolders",
            "DeleteItem",
            "DeleteFolder",
            "DeleteMultiple",
            "GiveInventory",
            "UpdateItem",
            "UpdateFolder",
            "JoinGroup",
            "LeaveGroup",
            "ScriptPermissionResponse"
        };

        // Methods

        private bool VerifySignature(User user, string querystring)
        {
            // Check that we have enough characters to avoid an ArgumentOutOfRangeException.
            // If we don't have at least this many, there's certainly no hash anyway.
            if (querystring.Length < 38) return false;

            // All this does the same job as the following on the client side:
            // var tohash = (++AjaxLife.SignedCallCount).toString() + querystring + AjaxLife.Signature;
            // var hash = md5(tohash);

            // First we have to remove the hash from the incoming string. We may assume the has is always at the end.
            // This makes the job easy - we just chop the end off. No parsing required.
            // MD5s are 128 bits, or 32 hex characters, so we chop off "&hash=00000000000000000000000000000000", which is
            // 38 characters.
            string receivedhash = querystring.Substring(querystring.Length - 32); // Grab the last 32 characters.
            querystring = querystring.Remove(querystring.Length - 38); // Strip the hash off.
            ++user.SignedCallCount; // Increment the call count to ensure the same hash can't be used multiple times.
            string tohash = user.SignedCallCount.ToString() + querystring + user.Signature; // Build the to hash string.
            string expectedhash = BitConverter.ToString(md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(tohash))).Replace("-", "").ToLower(); // Actually hash it.

            AjaxLife.Debug("SendMessage", "VerifySignature: Received hash " + receivedhash + ", expected " + expectedhash + " (based on '" + tohash + "')");
            // Check if they're equal.
            return (receivedhash == expectedhash);

        }

        public SendMessage(Dictionary<Guid, User> users) : base("send")
        {
            this.users = users;
        }

        public override OSD HandleRequest(IHttpRequest request)
        {
            OSD data = new OSDMap();

            try
            {
                GridClient client;
                AvatarTracker avatars;
                Events events;
                StreamReader reader = new StreamReader(request.Body);
                string qstring = reader.ReadToEnd();
                reader.Dispose();
                HttpForm POST = request.Form;
                // Pull out the session.
                if (!POST.Contains("sid"))
                {
                    throw new Exception("Need an SID.");
                }
                Guid guid = new Guid(POST["sid"].Value);
                User user = new User();
                lock (this.users)
                {
                    if (!this.users.ContainsKey(guid))
                    {
                        throw new Exception("Error: invalid SID");
                    }
                    user = this.users[guid];
                    client = user.Client;
                    avatars = user.Avatars;
                    events = user.Events;
                    user.LastRequest = DateTime.Now;
                }
                // Get the message type.
                string messagetype = POST["MessageType"].Value;

                // Check that the message is signed if it should be.
                if (Array.IndexOf(REQUIRED_SIGNATURES, messagetype) > -1)
                {
                    if (!VerifySignature(user, qstring))
                    {
                        throw new Exception("Error: Received hash and expected hash do not match.");
                    }
                }

                // Right. This file is fun. It takes information in POST paramaters and sends them to 
                // the server in the appropriate format. Some will return data immediately, some will return
                // keys to data that will arrive in the message queue, some return nothing but you get
                // something in the message queue later, and some return nother ever.
                // 
                // The joys of dealing with multiple bizarre message types.

                switch (messagetype)
                {
                    case "SpatialChat":
                        client.Self.Chat(POST["Message"].Value, int.Parse(POST["Channel"].Value), (ChatType)((byte)int.Parse(POST["Type"].Value)));
                        break;
                    case "SimpleInstantMessage":
                        if (POST.Contains("IMSessionID"))
                        {
                            client.Self.InstantMessage(new UUID(POST["Target"].Value), POST["Message"].Value, new UUID(POST["IMSessionID"].Value));
                        }
                        else
                        {
                            client.Self.InstantMessage(new UUID(POST["Target"].Value), POST["Message"].Value);
                        }
                        break;
                    case "GenericInstantMessage":
                        client.Self.InstantMessage(
                            client.Self.FirstName + " " + client.Self.LastName,
                            new UUID(POST["Target"].Value),
                            POST["Message"].Value,
                            new UUID(POST["IMSessionID"].Value),
                            (InstantMessageDialog)((byte)int.Parse(POST["Dialog"].Value)),
                            (InstantMessageOnline)int.Parse(POST["Online"].Value),
                            client.Self.SimPosition,
                            client.Network.CurrentSim.ID,
                            new byte[0]);
                        break;
                    case "NameLookup":
                        client.Avatars.RequestAvatarName(new UUID(POST["ID"].Value));
                        break;
                    case "Teleport":
                        {
                            ((OSDMap)data).Clear();
                            bool status;
                            if (POST.Contains("Landmark"))
                            {
                                status = client.Self.Teleport(new UUID(POST["Landmark"].Value));
                            }
                            else
                            {
                                status = client.Self.Teleport(POST["Sim"].Value, new Vector3(float.Parse(POST["X"].Value), float.Parse(POST["Y"].Value), float.Parse(POST["Z"].Value)));
                            }
                            if (status)
                            {
                                ((OSDMap)data).Add("Success", true);
                                ((OSDMap)data).Add("Sim", client.Network.CurrentSim.Name);
                                ((OSDMap)data).Add("Position", client.Self.SimPosition);
                            }
                            else
                            {
                                ((OSDMap)data).Add("Success", false);
                                ((OSDMap)data).Add("Reason", client.Self.TeleportMessage);
                            }
                        }
                        break;
                    case "GoHome":
                        client.Self.GoHome();
                        break;
                    case "GetPosition":
                        {
                            ((OSDMap)data).Clear();
                            ((OSDMap)data).Add("Sim", client.Network.CurrentSim.Name);
                            ((OSDMap)data).Add("Position", client.Self.SimPosition);
                        }
                        break;
                    case "RequestBalance":
                        client.Self.RequestBalance();
                        break;
                    case "GetStats":
                        {
                            ((OSDMap)data).Clear();
                            ((OSDMap)data).Add("FPS", client.Network.CurrentSim.Stats.FPS);
                            ((OSDMap)data).Add("TimeDilation", client.Network.CurrentSim.Stats.Dilation);
                            ((OSDMap)data).Add("Objects", client.Network.CurrentSim.Stats.Objects);
                            ((OSDMap)data).Add("ActiveScripts", client.Network.CurrentSim.Stats.ActiveScripts);
                            ((OSDMap)data).Add("Agents", client.Network.CurrentSim.Stats.Agents);
                            ((OSDMap)data).Add("ChildAgents", client.Network.CurrentSim.Stats.ChildAgents);
                            ((OSDMap)data).Add("AjaxLifeSessions", users.Count);
                            ((OSDMap)data).Add("PingSim", client.Network.CurrentSim.Stats.LastLag);
                            ((OSDMap)data).Add("IncomingBPS", client.Network.CurrentSim.Stats.IncomingBPS);
                            ((OSDMap)data).Add("OutgoingBPS", client.Network.CurrentSim.Stats.OutgoingBPS);
                            ((OSDMap)data).Add("DroppedPackets", client.Network.CurrentSim.Stats.ReceivedResends + client.Network.CurrentSim.Stats.ResentPackets);
                        }
                        break;
                    case "TeleportLureRespond":
                        client.Self.TeleportLureRespond(new UUID(POST["RequesterID"].Value), new UUID(POST["SessionID"].Value), bool.Parse(POST["Accept"].Value));
                        break;
                    case "GodlikeTeleportLureRespond":
                        {
                            UUID lurer = new UUID(POST["RequesterID"].Value);
                            UUID session = new UUID(POST["SessionID"].Value);
                            client.Self.InstantMessage(client.Self.Name, lurer, "", UUID.Random(), InstantMessageDialog.AcceptTeleport, InstantMessageOnline.Offline, client.Self.SimPosition, UUID.Zero, new byte[0]);
                            TeleportLureRequestPacket lure = new TeleportLureRequestPacket();
                            lure.Info.AgentID = client.Self.AgentID;
                            lure.Info.SessionID = client.Self.SessionID;
                            lure.Info.LureID = session;
                            lure.Info.TeleportFlags = (uint)TeleportFlags.ViaGodlikeLure;
                            client.Network.SendPacket(lure);
                        }
                        break;
                    case "FindPeople":
                        {
                            ((OSDMap)data).Clear();
                            ((OSDMap)data).Add("QueryID", client.Directory.StartPeopleSearch(POST["Search"].Value, int.Parse(POST["Start"].Value)));
                        }
                        break;
                    case "FindGroups":
                        {
                            ((OSDMap)data).Clear();
                            ((OSDMap)data).Add("QueryID", client.Directory.StartGroupSearch(POST["Search"].Value, int.Parse(POST["Start"].Value)));
                        }
                        break;
                    case "GetAgentData":
                        client.Avatars.RequestAvatarProperties(new UUID(POST["AgentID"].Value));
                        break;
                    case "StartAnimation":
                        client.Self.AnimationStart(new UUID(POST["Animation"].Value), false);
                        break;
                    case "StopAnimation":
                        client.Self.AnimationStop(new UUID(POST["Animation"].Value), true);
                        break;
                    case "SendAppearance":
                        client.Appearance.RequestSetAppearance();
                        break;
                    case "GetMapItems":
                        {
                            MapItemRequestPacket req = new MapItemRequestPacket();
                            req.AgentData.AgentID = client.Self.AgentID;
                            req.AgentData.SessionID = client.Self.SessionID;
                            GridRegion region;
                            client.Grid.GetGridRegion(POST["Region"].Value, GridLayerType.Objects, out region);
                            req.RequestData.RegionHandle = region.RegionHandle;
                            req.RequestData.ItemType = uint.Parse(POST["ItemType"].Value);
                            client.Network.SendPacket((Packet)req);
                        }
                        break;
                    case "GetMapBlocks":
                        {
                            MapBlockRequestPacket req = new MapBlockRequestPacket();
                            req.AgentData.AgentID = client.Self.AgentID;
                            req.AgentData.SessionID = client.Self.SessionID;
                            req.PositionData.MinX = ushort.Parse(POST["MinX"].Value);
                            req.PositionData.MinY = ushort.Parse(POST["MinY"].Value);
                            req.PositionData.MaxX = ushort.Parse(POST["MaxX"].Value);
                            req.PositionData.MaxY = ushort.Parse(POST["MaxY"].Value);
                            client.Network.SendPacket((Packet)req);
                        }
                        break;
                    case "FindRegion":
                        {
                            OpenMetaverse.Packets.MapNameRequestPacket packet = new OpenMetaverse.Packets.MapNameRequestPacket();
                            packet.NameData = new MapNameRequestPacket.NameDataBlock();
                            packet.NameData.Name = Utils.StringToBytes(POST["Name"].Value);
                            packet.AgentData.AgentID = client.Self.AgentID;
                            packet.AgentData.SessionID = client.Self.SessionID;
                            client.Network.SendPacket((Packet)packet);
                        }
                        break;
                    case "GetOfflineMessages":
                        {
                            RetrieveInstantMessagesPacket req = new RetrieveInstantMessagesPacket();
                            req.AgentData.AgentID = client.Self.AgentID;
                            req.AgentData.SessionID = client.Self.SessionID;
                            client.Network.SendPacket((Packet)req);
                        }
                        break;
                    case "GetFriendList":
                        {
                            InternalDictionary<UUID, FriendInfo> friends = client.Friends.FriendList;
                            List<Hashtable> friendlist = new List<Hashtable>();
                            data = new OSDArray();
                            friends.ForEach(delegate (FriendInfo friend)
                            {
                                OSDMap friendhash = new OSDMap();
                                friendhash.Add("ID", friend.UUID.ToString());
                                friendhash.Add("Name", friend.Name);
                                friendhash.Add("Online", friend.IsOnline);
                                friendhash.Add("MyRights", (int) friend.MyFriendRights);
                                friendhash.Add("TheirRights", (int)friend.TheirFriendRights);
                                ((OSDArray)data).Add(friendhash);
                            });
                        }
                        break;
                    case "ChangeRights":
                        {
                            UUID uuid = new UUID(POST["Friend"].Value);
                            client.Friends.GrantRights(uuid, (FriendRights)int.Parse(POST["Rights"].Value));
                        }
                        break;
                    case "RequestLocation":
                        client.Friends.MapFriend(new UUID(POST["Friend"].Value));
                        break;
                    case "RequestTexture":
                        {
                            // This one's confusing, so it gets some comments.
                            // First, we get the image's UUID.
                            UUID image = new UUID(POST["ID"].Value);
                            // We prepare a query to ask if S3 has it. HEAD only to avoid wasting
                            // GET requests and bandwidth.
                            bool exists = false;
                            // If we already know we have it, note this.
                            if (AjaxLife.CachedTextures.Contains(image))
                            {
                                exists = true;
                            }
                            else
                            {
                                // If we're using S3, check the S3 bucket
                                if (AjaxLife.USE_S3)
                                {
                                    // Otherwise, make that HEAD request and find out.
                                    HttpWebRequest webrequest = (HttpWebRequest)HttpWebRequest.Create(AjaxLife.TEXTURE_ROOT + image + ".png");
                                    webrequest.Method = "HEAD";
                                    webrequest.KeepAlive = false;
                                    webrequest.ReadWriteTimeout = 1000;
                                    webrequest.Timeout = 2500;
                                    HttpWebResponse response = null;
                                    try
                                    {
                                        response = (HttpWebResponse)webrequest.GetResponse();
                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {
                                            exists = true;
                                        }
                                    }
                                    catch (WebException e)
                                    {
                                        AjaxLife.Debug("SendMessage", "WebException (" + e.Status.ToString() + "): " + e.Message);
                                    }
                                    finally
                                    {
                                        if (response != null)
                                        {
                                            response.Close();
                                        }
                                    }
                                }
                                // If we aren't using S3, just check the texture cache.
                                else
                                {
                                    exists = File.Exists(AjaxLife.TEXTURE_CACHE + image.ToString() + ".png");
                                }
                            }
                            // If it exists, reply with Ready = true and the URL to find it at.
                            if (exists)
                            {
                                ((OSDMap)data).Add("Ready", true);
                                ((OSDMap)data).Add("URL", AjaxLife.TEXTURE_ROOT + image + ".png");
                            }
                            // If it doesn't, request the image from SL and note its lack of readiness.
                            // Notification will arrive later in the message queue.
                            else
                            {
                                client.Assets.RequestImage(image, new TextureDownloadCallback(events.Assets_TextureDownloadCallback));
                                ((OSDMap)data).Add("Ready", false);
                            }
                        }
                        break;
                    case "AcceptFriendship":
                        client.Friends.AcceptFriendship(client.Self.AgentID, new UUID(POST["IMSessionID"].Value));
                        break;
                    case "DeclineFriendship":
                        client.Friends.DeclineFriendship(client.Self.AgentID, new UUID(POST["IMSessionID"].Value));
                        break;
                    case "OfferFriendship":
                        client.Friends.OfferFriendship(new UUID(POST["Target"].Value));
                        break;
                    case "TerminateFriendship":
                        client.Friends.TerminateFriendship(new UUID(POST["Target"].Value));
                        break;
                    case "SendAgentMoney":
                        client.Self.GiveAvatarMoney(new UUID(POST["Target"].Value), int.Parse(POST["Amount"].Value));
                        break;
                    case "RequestAvatarList":
                        {
                            List<Hashtable> list = new List<Hashtable>();
                            data = new OSDArray();
                            foreach (KeyValuePair<uint, Avatar> pair in avatars.Avatars)
                            {
                                Avatar avatar = pair.Value;
                                OSDMap hash = new OSDMap();
                                hash.Add("Name", avatar.Name);
                                hash.Add("ID", avatar.ID);
                                hash.Add("LocalID", avatar.LocalID);
                                hash.Add("Position", avatar.Position);
                                //hash.Add("Rotation", avatar.Rotation);
                                hash.Add("Scale", avatar.Scale);
                                hash.Add("GroupName", avatar.GroupName);
                                ((OSDArray)data).Add(hash);
                            }
                        }
                        break;
                    case "LoadInventoryFolder":
                        client.Inventory.RequestFolderContents(new UUID(POST["UUID"].Value), client.Self.AgentID, true, true, InventorySortOrder.ByDate | InventorySortOrder.SystemFoldersToTop);
                        break;
                    case "RequestAsset":
                        {
                            try
                            {
                                UUID inventoryID = new UUID(POST["InventoryID"].Value);
                                client.Assets.RequestInventoryAsset(new UUID(POST["AssetID"].Value), inventoryID,
                                    UUID.Zero, new UUID(POST["OwnerID"].Value), (AssetType)int.Parse(POST["AssetType"].Value), false,
                                    delegate (AssetDownload transfer, OpenMetaverse.Assets.Asset asset) {
                                        events.Assets_OnAssetReceived(transfer, asset, inventoryID);
                                    }
                                );
                            }
                            catch // Try catching the error that sometimes gets thrown... but sometimes doesn't.
                            {

                            }
                        }
                        break;
                    case "SendTeleportLure":
                        client.Self.SendTeleportLure(new UUID(POST["Target"].Value), POST["Message"].Value);
                        break;
                    case "ScriptPermissionResponse":
                        client.Self.ScriptQuestionReply(client.Network.CurrentSim, new UUID(POST["ItemID"].Value), new UUID(POST["TaskID"].Value), (ScriptPermission)int.Parse(POST["Permissions"].Value));
                        break;
                    case "ScriptDialogReply":
                        {
                            ScriptDialogReplyPacket packet = new ScriptDialogReplyPacket();
                            packet.AgentData.AgentID = client.Self.AgentID;
                            packet.AgentData.SessionID = client.Self.SessionID;
                            packet.Data.ButtonIndex = int.Parse(POST["ButtonIndex"].Value);
                            packet.Data.ButtonLabel = Utils.StringToBytes(POST["ButtonLabel"].Value);
                            packet.Data.ChatChannel = int.Parse(POST["ChatChannel"].Value);
                            packet.Data.ObjectID = new UUID(POST["ObjectID"].Value);
                            client.Network.SendPacket((Packet)packet);
                        }
                        break;
                    case "SaveNotecard":
                        client.Inventory.RequestUploadNotecardAsset(Utils.StringToBytes(POST["AssetData"].Value), new UUID(POST["ItemID"].Value), new InventoryManager.InventoryUploadedAssetCallback(events.Inventory_OnNoteUploaded));
                        break;
                    case "CreateInventory":
                        client.Inventory.RequestCreateItem(new UUID(POST["Folder"].Value), POST["Name"].Value, POST["Description"].Value, (AssetType)int.Parse(POST["AssetType"].Value), UUID.Random(), (InventoryType)int.Parse(POST["InventoryType"].Value), PermissionMask.All, new InventoryManager.ItemCreatedCallback(events.Inventory_OnItemCreated));
                        break;
                    case "CreateFolder":
                        {
                            UUID folder = client.Inventory.CreateFolder(new UUID(POST["Parent"].Value), POST["Name"].Value);
                            ((OSDMap)data).Add("FolderID", folder);
                        }
                        break;
                    case "EmptyTrash":
                        client.Inventory.EmptyTrash();
                        break;
                    case "MoveItem":
                        client.Inventory.MoveItem(new UUID(POST["Item"].Value), new UUID(POST["TargetFolder"].Value), POST["NewName"].Value);
                        break;
                    case "MoveFolder":
                        client.Inventory.MoveFolder(new UUID(POST["Folder"].Value), new UUID(POST["NewParent"].Value));
                        break;
                    case "MoveItems":
                    case "MoveFolders":
                        {
                            Dictionary<UUID, UUID> dict = new Dictionary<UUID, UUID>();
                            string[] moves = POST["ToMove"].Value.Split(',');
                            for (int i = 0; i < moves.Length; ++i)
                            {
                                string[] move = moves[i].Split(' ');
                                dict.Add(new UUID(move[0]), new UUID(move[1]));
                            }
                            if (messagetype == "MoveItems")
                            {
                                client.Inventory.MoveItems(dict);
                            }
                            else if (messagetype == "MoveFolders")
                            {
                                client.Inventory.MoveFolders(dict);
                            }
                        }
                        break;
                    case "DeleteItem":
                        client.Inventory.RemoveItem(new UUID(POST["Item"].Value));
                        break;
                    case "DeleteFolder":
                        client.Inventory.RemoveFolder(new UUID(POST["Folder"].Value));
                        break;
                    case "DeleteMultiple":
                        {
                            string[] items = POST["Items"].Value.Split(',');
                            List<UUID> itemlist = new List<UUID>();
                            for (int i = 0; i < items.Length; ++i)
                            {
                                itemlist.Add(new UUID(items[i]));
                            }
                            string[] folders = POST["Folders"].Value.Split(',');
                            List<UUID> folderlist = new List<UUID>();
                            for (int i = 0; i < items.Length; ++i)
                            {
                                folderlist.Add(new UUID(folders[i]));
                            }
                            client.Inventory.Remove(itemlist, folderlist);
                        }
                        break;
                    case "GiveInventory":
                        {
                            client.Inventory.GiveItem(new UUID(POST["ItemID"].Value), POST["ItemName"].Value, (AssetType)int.Parse(POST["AssetType"].Value), new UUID(POST["Recipient"].Value), true);
                        }
                        break;
                    case "UpdateItem":
                        {
                            InventoryItem item = client.Inventory.FetchItem(new UUID(POST["ItemID"].Value), new UUID(POST["OwnerID"].Value), 1000);
                            if (POST.Contains("Name")) item.Name = POST["Name"].Value;
                            if (POST.Contains("Description")) item.Description = POST["Description"].Value;
                            if (POST.Contains("NextOwnerMask")) item.Permissions.NextOwnerMask = (PermissionMask)uint.Parse(POST["NextOwnerMask"].Value);
                            if (POST.Contains("SalePrice")) item.SalePrice = int.Parse(POST["SalePrice"].Value);
                            if (POST.Contains("SaleType")) item.SaleType = (SaleType)int.Parse(POST["SaleType"].Value); // This should be byte.Parse, but this upsets mono's compiler (CS1002)
                            client.Inventory.RequestUpdateItem(item);
                        }
                        break;
                    case "UpdateFolder":
                        {
                            UpdateInventoryFolderPacket packet = new UpdateInventoryFolderPacket();
                            packet.AgentData.AgentID = client.Self.AgentID;
                            packet.AgentData.SessionID = client.Self.SessionID;
                            packet.FolderData = new UpdateInventoryFolderPacket.FolderDataBlock[1];
                            packet.FolderData[0] = new UpdateInventoryFolderPacket.FolderDataBlock();
                            packet.FolderData[0].FolderID = new UUID(POST["FolderID"].Value);
                            packet.FolderData[0].ParentID = new UUID(POST["ParentID"].Value);
                            packet.FolderData[0].Type = sbyte.Parse(POST["Type"].Value);
                            packet.FolderData[0].Name = Utils.StringToBytes(POST["Name"].Value);
                            client.Network.SendPacket((Packet)packet);
                        }
                        break;
                    case "FetchItem":
                        client.Inventory.FetchItem(new UUID(POST["Item"].Value), new UUID(POST["Owner"].Value), 5000);
                        break;
                    case "ReRotate":
                        user.Rotation = -Math.PI;
                        break;
                    case "StartGroupIM":
                        AjaxLife.Debug("SendMessage", "RequestJoinGroupChat(" + POST["Group"].Value + ")");
                        client.Self.RequestJoinGroupChat(new UUID(POST["Group"].Value));
                        break;
                    case "GroupInstantMessage":
                        client.Self.InstantMessageGroup(new UUID(POST["Group"].Value), POST["Message"].Value);
                        break;
                    case "RequestGroupProfile":
                        client.Groups.RequestGroupProfile(new UUID(POST["Group"].Value));
                        break;
                    case "RequestGroupMembers":
                        client.Groups.RequestGroupMembers(new UUID(POST["Group"].Value));
                        break;
                    case "RequestGroupName":
                        client.Groups.RequestGroupName(new UUID(POST["ID"].Value));
                        break;
                    case "JoinGroup":
                        client.Groups.RequestJoinGroup(new UUID(POST["Group"].Value));
                        break;
                    case "LeaveGroup":
                        client.Groups.LeaveGroup(new UUID(POST["Group"].Value));
                        break;
                    case "RequestCurrentGroups":
                        client.Groups.RequestCurrentGroups();
                        break;
                    case "GetParcelID":
                        ((OSDMap)data).Add("LocalID", client.Parcels.GetParcelLocalID(client.Network.CurrentSim, new Vector3(float.Parse(POST["X"].Value), float.Parse(POST["Y"].Value), float.Parse(POST["Z"].Value))));
                        break;
                    case "RequestParcelProperties":
                        client.Parcels.RequestParcelProperties(client.Network.CurrentSim, int.Parse(POST["LocalID"].Value), int.Parse(POST["SequenceID"].Value));
                        break;
                }
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
