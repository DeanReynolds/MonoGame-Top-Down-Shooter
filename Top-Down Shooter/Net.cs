using Lidgren.Network;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Top_Down_Shooter
{
    public static class Net
    {
        public const int ConnectionTypeBitSizeIndex = 3;
        public const int ReadyToPlayTypeBitSizeIndex = 1;
        public const int DamageTypeBitSizeIndex = 1;
        public const int MapEditTypeBitsSizeIndex = 1;

        public static readonly int ServerPacketCountIndex = (Enum.GetValues(typeof(ServerPacket)).Length - 1);
        public static readonly int ClientPacketCountIndex = (Enum.GetValues(typeof(ClientPacket)).Length - 1);

        public static Player.NetState[] PlayerState { get; internal set; }

        public static NetPeer Peer { get; private set; }
        public static NetServer Server { get; private set; }
        public static NetClient Client { get; private set; }

        public enum ServerPacket { Connection, PlayerSync, PrimaryAttack, Damage, MapEdit }
        public enum ClientPacket { ReadyToPlay, PlayerSync, PrimaryAttack, Damage, MapEdit }

        private static event OnMessage _onUpdate;
        private static event OnDisconnect _onDisconnect;

        private delegate void OnMessage();
        private delegate void OnDisconnect();

        public static void Host(int port, int maxPlayers)
        {
            if (Peer != null)
                return;
            NetPeerConfiguration config = new NetPeerConfiguration(string.Format("game"))
            {
                Port = port,
                MaximumConnections = (maxPlayers - 1),
                
            };
            config.DisableMessageType(NetIncomingMessageType.DebugMessage);
            config.DisableMessageType(NetIncomingMessageType.DiscoveryRequest);
            config.DisableMessageType(NetIncomingMessageType.DiscoveryResponse);
            config.DisableMessageType(NetIncomingMessageType.Error);
            config.DisableMessageType(NetIncomingMessageType.ErrorMessage);
            config.DisableMessageType(NetIncomingMessageType.Receipt);
            config.DisableMessageType(NetIncomingMessageType.WarningMessage);
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            Peer = Server = new NetServer(config);
            _onUpdate += UpdateServer;
            _onDisconnect += DisconnectServer;
            Server.Start();
            PlayerState = new Player.NetState[maxPlayers - 1];
        }

        public static void Connect(string ip, int port)
        {
            if (Peer != null)
                return;
            NetPeerConfiguration config = new NetPeerConfiguration(string.Format("game"));
            Peer = Client = new NetClient(config);
            _onUpdate += UpdateClient;
            _onDisconnect += DisconnectClient;
            NetOutgoingMessage hailMsg = Client.CreateMessage();
            Client.Start();
            Client.Connect(ip, port, hailMsg);
        }

        public static void Update()
        {
            _onUpdate?.Invoke();
        }

        public static void Disconnect()
        {
            _onDisconnect?.Invoke();
            Peer = null;
        }

        private static void UpdateServer()
        {
            NetIncomingMessage msg;
            while ((msg = Peer.ReadMessage()) != null)
            {
                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    ClientPacket packet = (ClientPacket)msg.ReadRangedInteger(0, ClientPacketCountIndex);
                    Player player = Scenes.Game.GetPlayer(msg.SenderConnection);
                    if (PlayerState[player.ID] == Player.NetState.Playing)
                    {
                        if (packet == ClientPacket.PlayerSync)
                        {
                            player.Position = new Vector2(msg.ReadRangedInteger(0, Scenes.Game.MapWidth), msg.ReadRangedInteger(0, Scenes.Game.MapHeight));
                            player.Angle = Player.UnpackAngle(player.PackedAngle = msg.ReadRangedInteger(Player.MinAngle, Player.MaxAngle));
                            player.UpdateHitboxes();
                            PlayerSpatialHash.Update(player);
                        }
                        else if (packet == ClientPacket.PrimaryAttack)
                        {
                            player.Position = new Vector2(msg.ReadRangedInteger(0, Scenes.Game.MapWidth), msg.ReadRangedInteger(0, Scenes.Game.MapHeight));
                            player.Inventory[player.SelectedInventorySlot].AngleSpread = msg.ReadFloat();
                            NetOutgoingMessage msg2 = Server.CreateMessage();
                            msg.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.PrimaryAttack);
                            msg.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player.ID);
                            msg.WriteRangedInteger(0, Scenes.Game.MapWidth, (int)player.Position.X);
                            msg.WriteRangedInteger(0, Scenes.Game.MapHeight, (int)player.Position.Y);
                            Scenes.Game.PrimaryAttack(player, out float angleSpread);
                            msg.Write(angleSpread);
                            Server.SendToAll(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                            //Vector2 bulletStart = new Vector2(msg.ReadRangedInteger(0, Scenes.Game.MapWidth), msg.ReadRangedInteger(0, Scenes.Game.MapHeight));
                            ////while (msg.Position < msg.LengthBits)
                            ////    Scenes.Game.Bullets.Add(new Bullet(player.Position, new Vector2(msg.ReadRangedInteger(0, Scenes.Game.MapWidth), msg.ReadRangedInteger(0, Scenes.Game.MapHeight))));
                            //while (msg.Position < msg.LengthBits)
                            //{
                            //    float bulletAngle = Player.UnpackAngle(msg.ReadRangedInteger(-Player.AnglesOver2, Player.AnglesOver2));
                            //    float bulletRange = msg.ReadRangedInteger(0, 3000);
                            //    Scenes.Game.Bullets.Add(new Bullet(player.Position, new Vector2((float)(bulletStart.X + (Math.Cos(bulletAngle) * bulletRange)), (float)(bulletStart.Y + (Math.Sin(bulletAngle) * bulletRange)))));
                            //}
                        }
                        else if (packet == ClientPacket.Damage)
                        {
                            int type = msg.ReadRangedInteger(0, DamageTypeBitSizeIndex);
                            if (type == 0)
                            {
                                Player victim = Scenes.Game.GetPlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                                int damage = msg.ReadRangedInteger(1, victim.Health);
                                BulletHitInfo.HitTypes hitType = (BulletHitInfo.HitTypes)msg.ReadRangedInteger(0, BulletHitInfo.HitTypesCountIndex);
                                NetOutgoingMessage msg2 = Server.CreateMessage();
                                msg.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.Damage);
                                msg.WriteRangedInteger(0, DamageTypeBitSizeIndex, 0);
                                msg.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player.ID);
                                msg.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, victim.ID);
                                msg.WriteRangedInteger(1, victim.Health, damage);
                                msg.WriteRangedInteger(0, BulletHitInfo.HitTypesCountIndex, (int)hitType);
                                Server.SendToAll(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                                victim.TakeDamage(damage, player, hitType);
                            }
                            else if (type == 1)
                            {
                                NetOutgoingMessage msg2 = Server.CreateMessage();
                                msg.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.Damage);
                                msg.WriteRangedInteger(0, DamageTypeBitSizeIndex, 1);
                                msg.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player.ID);
                                Server.SendToAll(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                                player.Die();
                            }
                        }
                        else if (packet == ClientPacket.MapEdit)
                        {
                            int type = msg.ReadRangedInteger(0, MapEditTypeBitsSizeIndex);
                            if (type == 0)
                            {
                                int tileX = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesWidthIndex);
                                int tileY = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesHeightIndex);
                                int tileId = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesetCountIndex);
                                Scenes.Game.SetTile(tileX, tileY, tileId);
                                NetOutgoingMessage msg2 = Server.CreateMessage();
                                msg2.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.MapEdit);
                                msg2.WriteRangedInteger(0, MapEditTypeBitsSizeIndex, 1);
                                msg2.WriteRangedInteger(0, Scenes.Game.Map.TilesWidthIndex, tileX);
                                msg2.WriteRangedInteger(0, Scenes.Game.Map.TilesHeightIndex, tileY);
                                msg2.WriteRangedInteger(0, Scenes.Game.Map.TilesetCountIndex, tileId);
                                Server.SendToAll(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                            }
                            else if (type == 1)
                            {
                                int tileX = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesWidthIndex);
                                int tileY = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesHeightIndex);
                                if (Scenes.Game.Map.IsWall(tileX, tileY))
                                    Scenes.Game.DeactivateWallBody(tileX, tileY);
                                else if (Scenes.Game.Map.IsObstacle(tileX, tileY))
                                    Scenes.Game.DeactivateObstacleBody(tileX, tileY);
                                Scenes.Game.Map.RemoveTile(tileX, tileY);
                                NetOutgoingMessage msg2 = Server.CreateMessage();
                                msg2.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.MapEdit);
                                msg2.WriteRangedInteger(0, MapEditTypeBitsSizeIndex, 1);
                                msg2.WriteRangedInteger(0, Scenes.Game.Map.TilesWidthIndex, tileX);
                                msg2.WriteRangedInteger(0, Scenes.Game.Map.TilesHeightIndex, tileY);
                                Server.SendToAll(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                            }
                        }
                    }
                    else if (packet == ClientPacket.ReadyToPlay)
                    {
                        int type = msg.ReadRangedInteger(0, ReadyToPlayTypeBitSizeIndex);
                        if (type == 0)
                            PlayerState[player.ID] = Player.NetState.Playing;
                        //else if (type == 1)
                        //{
                        //    Console.WriteLine("t1");
                        //    List<NetConnection> connections = GetPlayingConnections(msg.SenderConnection);
                        //    if (connections.Count > 0)
                        //    {
                        //        NetOutgoingMessage msg2 = Server.CreateMessage();
                        //        msg.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.Connection);
                        //        msg.WriteRangedInteger(0, ConnectionTypeBitSizeIndex, 3);
                        //        msg.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player.ID);
                        //        Server.SendMessage(msg2, connections, NetDeliveryMethod.ReliableOrdered, 0);
                        //    }
                        //    player.Respawn();
                        //}
                    }
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    NetConnectionStatus connectionStatus = msg.SenderConnection.Status;
                    Player player = Scenes.Game.GetPlayer(msg.SenderConnection);
                    if (connectionStatus == NetConnectionStatus.Connected)
                    {
                        PlayerSpatialHash.Add(player);
                        NetOutgoingMessage msg2 = Server.CreateMessage();
                        msg2.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.Connection);
                        msg2.WriteRangedInteger(0, ConnectionTypeBitSizeIndex, 2);
                        for (int i = 0; i < Scenes.Game.Players.Length; i++)
                        {
                            if (i == player.ID)
                                continue;
                            Player player2 = Scenes.Game.GetPlayer(i);
                            if (player2 == null)
                                continue;
                            msg2.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player2.ID);
                            msg2.WriteRangedInteger(0, Scenes.Game.MapWidth, (int)player2.Position.X);
                            msg2.WriteRangedInteger(0, Scenes.Game.MapHeight, (int)player2.Position.Y);
                            msg2.WriteRangedInteger(Player.MinAngle, Player.MaxAngle, player2.PackedAngle);
                            msg2.Write(player2.RandomSeed);
                            msg2.Write(player2.Dead);
                            if (!player2.Dead)
                                msg2.WriteRangedInteger(0, 1000, player2.Health);
                        }
                        Server.SendMessage(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                    }
                    else if (connectionStatus == NetConnectionStatus.Disconnected)
                    {
                        NetOutgoingMessage msg2 = Server.CreateMessage();
                        msg2.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.Connection);
                        msg2.WriteRangedInteger(0, ConnectionTypeBitSizeIndex, 1);
                        msg2.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player.ID);
                        Server.SendToAll(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                        Scenes.Game.RemovePlayer(msg.SenderConnection);
                        PlayerState[player.ID] = Player.NetState.Disconnected;
                    }
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionApproval)
                {
                    Player player = Scenes.Game.AddPlayer(msg.SenderConnection);
                    if (player != null)
                    {
                        NetOutgoingMessage msg2 = Server.CreateMessage();
                        msg2.Write((byte)Scenes.Game.PlayerCountIndex);
                        msg2.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player.ID);
                        msg2.Write(player.RandomSeed);
                        msg.SenderConnection.Approve(msg2);
                        msg2 = Server.CreateMessage();
                        msg2.WriteRangedInteger(0, ServerPacketCountIndex, (int)ServerPacket.Connection);
                        msg2.WriteRangedInteger(0, ConnectionTypeBitSizeIndex, 0);
                        msg2.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, player.ID);
                        msg2.Write(player.RandomSeed);
                        Server.SendToAll(msg2, msg.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
                        PlayerState[player.ID] = Player.NetState.Connecting;
                    }
                }
            }
        }

        private static void DisconnectServer()
        {
            NetServer server = (NetServer)Peer;
            server.Shutdown("quit");
            _onUpdate -= UpdateServer;
            _onDisconnect -= DisconnectServer;
        }

        private static void UpdateClient()
        {
            NetIncomingMessage msg;
            while ((msg = Peer.ReadMessage()) != null)
            {
                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    ServerPacket packet = (ServerPacket)msg.ReadRangedInteger(0, ServerPacketCountIndex);
                    if (packet == ServerPacket.PlayerSync)
                        while (msg.Position < msg.LengthBits)
                        {
                            Player player = Scenes.Game.GetPlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                            Vector2 position = new Vector2(msg.ReadRangedInteger(0, Scenes.Game.MapWidth), msg.ReadRangedInteger(0, Scenes.Game.MapHeight));
                            int packedAngle = msg.ReadRangedInteger(Player.MinAngle, Player.MaxAngle);
                            if (player != null)
                            {
                                player.Position = position;
                                player.Angle = Player.UnpackAngle(player.PackedAngle = packedAngle);
                                player.UpdateHitboxes();
                                PlayerSpatialHash.Update(player);
                            }
                            else
                                continue;
                        }
                    else if (packet == ServerPacket.PrimaryAttack)
                    {
                        Player player = Scenes.Game.GetPlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                        player.Position = new Vector2(msg.ReadRangedInteger(0, Scenes.Game.MapWidth), msg.ReadRangedInteger(0, Scenes.Game.MapHeight));
                        player.Inventory[player.SelectedInventorySlot].AngleSpread = msg.ReadFloat();
                        Scenes.Game.PrimaryAttack(player, out _);
                    }
                    else if (packet == ServerPacket.Damage)
                    {
                        int type = msg.ReadRangedInteger(0, DamageTypeBitSizeIndex);
                        Console.WriteLine(type);
                        if (type == 0)
                        {
                            Player player = Scenes.Game.GetPlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                            Player victim = Scenes.Game.GetPlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                            int damage = msg.ReadRangedInteger(1, victim.Health);
                            BulletHitInfo.HitTypes hitType = (BulletHitInfo.HitTypes)msg.ReadRangedInteger(0, BulletHitInfo.HitTypesCountIndex);
                            victim.TakeDamage(damage, player, hitType);
                        }
                        else if (type == 1)
                        {
                            Player player = Scenes.Game.GetPlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                            player.Die();
                        }
                    }
                    else if (packet == ServerPacket.Connection)
                    {
                        int type = msg.ReadRangedInteger(0, ConnectionTypeBitSizeIndex);
                        if (type == 0)
                        {
                            Player player = new Player(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                            player.Random = new Random(player.RandomSeed = msg.ReadInt32());
                            Scenes.Game.SetPlayer(player.ID, player);
                        }
                        else if (type == 1)
                            Scenes.Game.RemovePlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                        else if (type == 2)
                        {
                            while (msg.Position < msg.LengthBits)
                            {
                                Player player = new Player(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                                player.Position = new Vector2(msg.ReadRangedInteger(0, Scenes.Game.MapWidth), msg.ReadRangedInteger(0, Scenes.Game.MapHeight));
                                player.SnapSoftPosition();
                                player.Angle = Player.UnpackAngle(player.PackedAngle = msg.ReadRangedInteger(Player.MinAngle, Player.MaxAngle));
                                player.Random = new Random(player.RandomSeed = msg.ReadInt32());
                                player.Dead = msg.ReadBoolean();
                                if (!player.Dead)
                                    player.Health = msg.ReadRangedInteger(0, 1000);
                                Scenes.Game.SetPlayer(player.ID, player);
                                PlayerSpatialHash.Add(player);
                            }
                            NetOutgoingMessage msg2 = Client.CreateMessage();
                            msg2.WriteRangedInteger(0, ClientPacketCountIndex, (int)ClientPacket.ReadyToPlay);
                            msg2.WriteRangedInteger(0, ReadyToPlayTypeBitSizeIndex, 0);
                            Client.SendMessage(msg2, NetDeliveryMethod.ReliableOrdered);
                        }
                        else if (type == 3)
                        {
                            Player player = Scenes.Game.GetPlayer(msg.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                            player.Respawn();
                        }
                    }
                    else if (packet == ServerPacket.MapEdit)
                    {
                        int type = msg.ReadRangedInteger(0, MapEditTypeBitsSizeIndex);
                        if (type == 0)
                        {
                            int tileX = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesWidthIndex);
                            int tileY = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesHeightIndex);
                            int tileId = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesetCountIndex);
                            Scenes.Game.SetTile(tileX, tileY, tileId);
                        }
                        else if (type == 1)
                        {
                            int tileX = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesWidthIndex);
                            int tileY = msg.ReadRangedInteger(0, Scenes.Game.Map.TilesHeightIndex);
                            if (Scenes.Game.Map.IsWall(tileX, tileY))
                                Scenes.Game.DeactivateWallBody(tileX, tileY);
                            else if (Scenes.Game.Map.IsObstacle(tileX, tileY))
                                Scenes.Game.DeactivateObstacleBody(tileX, tileY);
                            Scenes.Game.Map.RemoveTile(tileX, tileY);
                        }
                    }
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    NetConnectionStatus connectionStatus = (NetConnectionStatus)msg.ReadByte();
                    if (connectionStatus == NetConnectionStatus.Connected)
                    {
                        Game1.Scene = new Scenes.Game(msg.SenderConnection.RemoteHailMessage.ReadByte() + 1);
                        Player player = new Player(msg.SenderConnection.RemoteHailMessage.ReadRangedInteger(0, Scenes.Game.PlayerCountIndex));
                        PlayerSpatialHash.Add(player);
                        Scenes.Game.Self = Scenes.Game.SetPlayer(player.ID, player);
                        Scenes.Game.Self.Random = new Random(Scenes.Game.Self.RandomSeed = msg.SenderConnection.RemoteHailMessage.ReadInt32());
                    }
                    else if (connectionStatus == NetConnectionStatus.Disconnected)
                    {
                        // TODO
                    }
                }
            }
        }

        private static void DisconnectClient()
        {
            NetClient client = (NetClient)Peer;
            client.Disconnect("quit");
            _onUpdate -= UpdateClient;
            _onDisconnect -= DisconnectClient;
        }

        public static List<NetConnection> GetPlayingConnections()
        {
            List<NetConnection> connectionsToSend = Net.Server.Connections;
            for (int i = 0; i < connectionsToSend.Count; i++)
            {
                Player player = Scenes.Game.GetPlayer(connectionsToSend[i]);
                if (player != null)
                    if (PlayerState[player.ID] != Player.NetState.Playing)
                        connectionsToSend.RemoveAt(i--);
            }
            return connectionsToSend;
        }

        public static List<NetConnection> GetPlayingConnections(NetConnection excempt)
        {
            List<NetConnection> connectionsToSend = Net.Server.Connections;
            for (int i = 0; i < connectionsToSend.Count; i++)
            {
                Player player = Scenes.Game.GetPlayer(connectionsToSend[i]);
                if (player != null)
                    if ((PlayerState[player.ID] != Player.NetState.Playing) || (player.Connection == excempt))
                        connectionsToSend.RemoveAt(i--);
            }
            return connectionsToSend;
        }
    }
}