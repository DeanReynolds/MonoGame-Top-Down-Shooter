using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VelcroPhysics.Collision.Narrowphase;
using VelcroPhysics.Dynamics;
using VelcroPhysics.Extensions.DebugView;
using VelcroPhysics.Factories;
using VelcroPhysics.Shared;
using VelcroPhysics.Utilities;
using static VelcroPhysics.DebugViews.MonoGame.DebugView;

namespace Top_Down_Shooter.Scenes
{
    public class Game : Scene
    {
        public const int PixelsPerMeter = 100;
        public const long SyncRate = (TimeSpan.TicksPerSecond / 60);
        public const int BulletMaxOffset = 100;

        public static Player Self { get; internal set; }

        public static Camera Camera { get; private set; }
        public static Player[] Players { get; private set; }
        public static Dictionary<NetConnection, Player> PlayersByConnection { get; private set; }
        public static int PlayerCount { get; private set; }
        public static int PlayerCountIndex { get; private set; }
        public static Map Map { get; private set; }
        public static int MapWidth { get; private set; }
        public static int MapHeight { get; private set; }
        public static float MapWidthSim { get; private set; }
        public static float MapHeightSim { get; private set; }
        public static int BulletMinX { get; private set; }
        public static int BulletMaxX { get; private set; }
        public static int BulletMinY { get; private set; }
        public static int BulletMaxY { get; private set; }
        public static World World { get; private set; }
        public static Body Body { get; private set; }
        public static Dictionary<string, GunStats> Guns { get; private set; }
        public static List<Bullet> Bullets { get; private set; }
        public static BasicEffect _basicEffect;

        private static Queue<Body> _wallPoolInactive;
        private static Dictionary<Point, Body> _wallPoolActive;
        private static Queue<Body> _obstaclePoolInactive;
        private static Dictionary<Point, Body> _obstaclePoolActive;
        private static event OnUpdate _onUpdate;
        private static long _syncTimer;
        private static INI _gunsIni;
        private static VelcroPhysics.DebugViews.MonoGame.DebugView _debugView;
        private static double _sunAngleTimer;
        private static float _sunAngle;

        private delegate void OnUpdate(GameTime gameTime);

        public Game(int maxPlayers)
        {
            _gunsIni = INI.ReadStream(Assembly.GetExecutingAssembly().GetManifestResourceStream("Top_Down_Shooter.Content.Guns.ini"));
            Guns = new Dictionary<string, GunStats>();
            foreach (string key in _gunsIni.Nodes.Keys)
            {
                //Console.WriteLine(string.Format("{0} = {1}", key, _gunsIni.Nodes[key]));
                string name = key.Split('.')[0];
                if (Guns.ContainsKey(name))
                    continue;
                GunStats gunStats = new GunStats
                {
                    InventorySlot = Convert.ToByte(_gunsIni.Nodes[string.Format("{0}.{1}", name, "inv_slot")]),
                    BulletsPerShot = Convert.ToByte(_gunsIni.Nodes[string.Format("{0}.{1}", name, "bullets_per_shot")]),
                    RoundsPerSecond = (1d / Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "rounds_per_second")])),
                    AngleSpreadInitial = MathHelper.ToRadians(Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "angle_spread_initial")])),
                    AngleSpreadWidenMax = MathHelper.ToRadians(Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "angle_spread_widen_max")])),
                    AngleSpreadWidenRateInShots = Convert.ToByte(_gunsIni.Nodes[string.Format("{0}.{1}", name, "angle_spread_widen_rate")]),
                    AngleSpreadWidenCooldownInSeconds = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "angle_spread_widen_cooldown")]),
                    MaxDamagePerBullet = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "damage")]),
                    DamageKnockoffPerMeter = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "damage_knockoff_per_meter")]),
                    DamageKnockoffPerPlayerHeadHit = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "damage_knockoff_per_player_head_hit")]),
                    DamageKnockoffPerPlayerShoulderHit = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "damage_knockoff_per_player_shoulder_hit")]),
                    MaxTotalAmmo = Convert.ToUInt16(_gunsIni.Nodes[string.Format("{0}.{1}", name, "total_ammo")]),
                    MaxAmmoInClip = Convert.ToUInt16(_gunsIni.Nodes[string.Format("{0}.{1}", name, "clip_ammo")]),
                    KickVisualInitial = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "kick_visual_initial")]),
                    KickVisualLead = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "kick_visual_lead")]),
                    KickVisualMax = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "kick_visual_max")]),
                    KickPhysicalInitial = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "kick_physical_initial")]),
                    KickPhysicalLead = Convert.ToSingle(_gunsIni.Nodes[string.Format("{0}.{1}", name, "kick_physical_lead")]),
                };
                float angleDifMinMax = Math.Abs(gunStats.AngleSpreadWidenMax - gunStats.AngleSpreadInitial);
                gunStats.AngleSpreadWidenRateIncreasePerShot = (angleDifMinMax / gunStats.AngleSpreadWidenRateInShots);
                gunStats.AngleSpreadWidenCooldownDecreasePerSecond = (angleDifMinMax / gunStats.AngleSpreadWidenCooldownInSeconds);
                gunStats.RangePerBullet = MathHelper.Min(3000, (((gunStats.MaxDamagePerBullet / gunStats.DamageKnockoffPerMeter) * PixelsPerMeter) - 1));
                gunStats.KickVisualRecoverRate = (gunStats.AngleSpreadWidenCooldownInSeconds / gunStats.KickVisualMax);
                //Console.WriteLine(gunStats.AngleSpread + " - " + gunStats.AnglePerBullet + " - " + gunStats.MaxSpreadPerBullet);
                Guns.Add(name, gunStats);
            }
            ConvertUnits.SetDisplayUnitToSimUnitRatio(PixelsPerMeter);
            _basicEffect = new BasicEffect(Program.Game.GraphicsDevice)
            {
                VertexColorEnabled = true
            };
            World = new World(Vector2.Zero);
            Map = Map.Load(".\\de_dust2.map");
            GenerateWallPool(30);
            GenerateObstaclePool(30);
            BulletMinX = BulletMinY = -BulletMaxOffset;
            BulletMaxX = ((MapWidth = (Map.TilesWidth * Map.TileSize)) + BulletMaxOffset);
            MapWidthSim = ConvertUnits.ToSimUnits(MapWidth);
            BulletMaxY = ((MapHeight = (Map.TilesHeight * Map.TileSize)) + BulletMaxOffset);
            MapHeightSim = ConvertUnits.ToSimUnits(MapHeight);
            _debugView = new VelcroPhysics.DebugViews.MonoGame.DebugView(World);
            _debugView.DefaultShapeColor = Color.White;
            _debugView.SleepingShapeColor = Color.LightGray;
            _debugView.Flags = (DebugViewFlags.PerformanceGraph | DebugViewFlags.Shape | DebugViewFlags.ContactPoints);
            _debugView.LoadContent(Program.Game.GraphicsDevice, Program.Game.Services.GetService<ContentManager>());
            PlayerCountIndex = ((PlayerCount = maxPlayers) - 1);
            Players = new Player[PlayerCount];
            PlayersByConnection = new Dictionary<NetConnection, Player>(Players.Length);
            PlayerSpatialHash.Clear();
            Body = BodyFactory.CreateCircle(World, ConvertUnits.ToSimUnits(Player.BodyRadius), 1, Vector2.Zero, BodyType.Dynamic, null);
            Body.FixedRotation = true;
            Body.LinearDamping = 10;
            Camera = new Camera(0, Game1.VirtualScale);
            Bullets = new List<Bullet>(50);
            _sunAngleTimer = .1;
        }

        public override void Update(GameTime gameTime)
        {
            World.Step(Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, (1 / 30f)));
            for (int i = 0; i < Bullets.Count; i++)
            {
                Bullets[i].Update(gameTime);
                if (Bullets[i].Opacity <= 0)
                    Bullets.RemoveAt(i--);
            }
            if (Program.Game.IsActive)
            {
                if (!Self.Dead)
                {
                    float movSpeed = (float)(100 * gameTime.ElapsedGameTime.TotalSeconds);
                    if (Keyboard.Holding(Keys.W))
                        Body.ApplyForce(new Vector2(0, -movSpeed));
                    if (Keyboard.Holding(Keys.S))
                        Body.ApplyForce(new Vector2(0, movSpeed));
                    if (Keyboard.Holding(Keys.A))
                        Body.ApplyForce(new Vector2(-movSpeed, 0));
                    if (Keyboard.Holding(Keys.D))
                        Body.ApplyForce(new Vector2(movSpeed, 0));
                    if (Keyboard.Holding(Keys.Home))
                        Body.Position = ConvertUnits.ToSimUnits(new Vector2(970, 520));
                    Body.Position = new Vector2(MathHelper.Clamp(Body.Position.X, 0, MapWidthSim), MathHelper.Clamp(Body.Position.Y, 0, MapHeightSim));
                    Vector2 bodyPosition = new Vector2((int)Math.Round(MathHelper.Clamp(ConvertUnits.ToDisplayUnits(Body.Position.X), 0, MapWidth)), (int)Math.Round(MathHelper.Clamp(ConvertUnits.ToDisplayUnits(Body.Position.Y), 0, MapHeight)));
                    MouseState mouseState = Mouse.GetState();
                    Camera.UpdateMousePosition(mouseState);
                    float lastAngle = Self.Angle;
                    Self.Angle = Player.UnpackAngle(Self.PackedAngle = Player.PackAngle(MathHelper.WrapAngle((float)Math.Atan2((Camera.MousePosition.Y - Self.Position.Y), (Camera.MousePosition.X - Self.Position.X)))));
                    if (Self.Position != bodyPosition)
                    {
                        Camera.Position = Self.Position = bodyPosition;
                        Self.UpdateHitboxes();
                        Self.SnapSoftPosition();
                        PlayerSpatialHash.Update(Self);
                        Map.ShadowsNeedRebake = true;
                    }
                    else if (lastAngle != Self.Angle)
                        Self.UpdateHitboxes();
                    //_camera.X = MathHelper.Clamp(_camera.X, virtualWidthOver2, ((_world.TilesWidth * Map.TileSize) - virtualWidthOver2));
                    //_camera.Y = MathHelper.Clamp(_camera.X, virtualHeightOver2, ((_world.TilesWidth * Map.TileSize) - virtualHeightOver2));
                    int mouseTileX = -1;
                    int mouseTileY = -1;
                    if (mouseState.LeftButton == ButtonState.Pressed)
                    {
                        if (mouseTileX == -1)
                            mouseTileX = (int)(Camera.MousePosition.X / Map.TileSize);
                        if (mouseTileY == -1)
                            mouseTileY = (int)(Camera.MousePosition.Y / Map.TileSize);
                        if (Map.InTileBounds(mouseTileX, mouseTileY))
                        {
                            SetTile(mouseTileX, mouseTileY, 1);
                            if (Net.Server != null)
                            {
                                NetOutgoingMessage msg = Net.Server.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.MapEdit);
                                msg.WriteRangedInteger(0, Net.MapEditTypeBitsSizeIndex, 0);
                                msg.WriteRangedInteger(0, Map.TilesWidthIndex, mouseTileX);
                                msg.WriteRangedInteger(0, Map.TilesHeightIndex, mouseTileY);
                                msg.WriteRangedInteger(0, Map.TilesetCountIndex, 1);
                                Net.Server.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                            else if (Net.Client != null)
                            {
                                NetOutgoingMessage msg = Net.Client.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ClientPacketCountIndex, (int)Net.ClientPacket.MapEdit);
                                msg.WriteRangedInteger(0, Net.MapEditTypeBitsSizeIndex, 0);
                                msg.WriteRangedInteger(0, Map.TilesWidthIndex, mouseTileX);
                                msg.WriteRangedInteger(0, Map.TilesHeightIndex, mouseTileY);
                                msg.WriteRangedInteger(0, Map.TilesetCountIndex, 1);
                                Net.Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                        }
                        if (PrimaryAttack(Self, out float angleSpread))
                            if (Net.Server != null)
                            {
                                NetOutgoingMessage msg = Net.Server.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.PrimaryAttack);
                                msg.WriteRangedInteger(0, PlayerCountIndex, Self.ID);
                                msg.WriteRangedInteger(0, MapWidth, (int)Self.Position.X);
                                msg.WriteRangedInteger(0, MapHeight, (int)Self.Position.Y);
                                msg.Write(angleSpread);
                                Net.Server.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                            else if (Net.Client != null)
                            {
                                NetOutgoingMessage msg = Net.Client.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ClientPacketCountIndex, (int)Net.ClientPacket.PrimaryAttack);
                                msg.WriteRangedInteger(0, MapWidth, (int)Self.Position.X);
                                msg.WriteRangedInteger(0, MapHeight, (int)Self.Position.Y);
                                msg.Write(angleSpread);
                                Net.Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                    }
                    if (mouseState.RightButton == ButtonState.Pressed)
                    {
                        if (mouseTileX == -1)
                            mouseTileX = (int)(Camera.MousePosition.X / Map.TileSize);
                        if (mouseTileY == -1)
                            mouseTileY = (int)(Camera.MousePosition.Y / Map.TileSize);
                        if (Map.InTileBounds(mouseTileX, mouseTileY))
                        {
                            if (Map.IsWall(mouseTileX, mouseTileY))
                                DeactivateWallBody(mouseTileX, mouseTileY);
                            else if (Map.IsObstacle(mouseTileX, mouseTileY))
                                DeactivateObstacleBody(mouseTileX, mouseTileY);
                            Map.RemoveTile(mouseTileX, mouseTileY);
                            if (Net.Server != null)
                            {
                                NetOutgoingMessage msg = Net.Server.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.MapEdit);
                                msg.WriteRangedInteger(0, Net.MapEditTypeBitsSizeIndex, 1);
                                msg.WriteRangedInteger(0, Map.TilesWidthIndex, mouseTileX);
                                msg.WriteRangedInteger(0, Map.TilesHeightIndex, mouseTileY);
                                Net.Server.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                            else if (Net.Client != null)
                            {
                                NetOutgoingMessage msg = Net.Client.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ClientPacketCountIndex, (int)Net.ClientPacket.MapEdit);
                                msg.WriteRangedInteger(0, Net.MapEditTypeBitsSizeIndex, 1);
                                msg.WriteRangedInteger(0, Map.TilesWidthIndex, mouseTileX);
                                msg.WriteRangedInteger(0, Map.TilesHeightIndex, mouseTileY);
                                Net.Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                        }
                    }
                }
            }
            if (Game1.DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].SelectedOption?.Text == "Off")
            {
                string t = Game1.DebugMenu.Groups["D2D"].Groups["Shadows"].Items["Angle"].Text;
                Map.SetShadowAngle(MathHelper.ToRadians(Convert.ToInt32(Game1.DebugMenu.Groups["D2D"].Groups["Shadows"].Items["Angle"].SelectedOption?.Text)));
            }
            else
            {
                _sunAngleTimer -= gameTime.ElapsedGameTime.TotalSeconds;
                if (_sunAngleTimer < 0)
                {
                    string speedText = Game1.DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].SelectedOption?.Text;
                    _sunAngleTimer += (Convert.ToInt32(speedText.Substring(0, (speedText.Length - 3))) / 1000d);
                    _sunAngle = MathHelper.WrapAngle(_sunAngle + MathHelper.ToRadians((float)(100 * gameTime.ElapsedGameTime.TotalSeconds)));
                    Map.SetShadowAngle(_sunAngle);
                }
            }
            if (Self.OldPosition != Self.Position)
            {
                const int bodyRadius = 20;
                HashSet<Point> wallsToDeactivate = new HashSet<Point>();
                HashSet<Point> obstaclesToDeactivate = new HashSet<Point>();
                int minXTile = (int)((Self.OldPosition.X - bodyRadius) / PlayerSpatialHash.Size);
                int minYTile = (int)((Self.OldPosition.Y - bodyRadius) / PlayerSpatialHash.Size);
                int maxXTile = (int)((Self.OldPosition.X + bodyRadius) / PlayerSpatialHash.Size);
                int maxYTile = (int)((Self.OldPosition.Y + bodyRadius) / PlayerSpatialHash.Size);
                for (int x = minXTile; x <= maxXTile; x++)
                    for (int y = minYTile; y <= maxYTile; y++)
                    {
                        int chunkX = (x >> Map.ChunkSizeBits);
                        int chunkY = (y >> Map.ChunkSizeBits);
                        int chunkTileX = (x & Map.ChunkSizeModulo);
                        int chunkTileY = (y & Map.ChunkSizeModulo);
                        if (Map.InTileBounds(x, y))
                        {
                            if (Map.IsWall(chunkX, chunkY, chunkTileX, chunkTileY))
                                wallsToDeactivate.Add(new Point(x, y));
                            else if (Map.IsObstacle(chunkX, chunkY, chunkTileX, chunkTileY))
                                obstaclesToDeactivate.Add(new Point(x, y));
                        }
                    }
                minXTile = (int)((Self.Position.X - bodyRadius) / PlayerSpatialHash.Size);
                minYTile = (int)((Self.Position.Y - bodyRadius) / PlayerSpatialHash.Size);
                maxXTile = (int)((Self.Position.X + bodyRadius) / PlayerSpatialHash.Size);
                maxYTile = (int)((Self.Position.Y + bodyRadius) / PlayerSpatialHash.Size);
                for (int x = minXTile; x <= maxXTile; x++)
                    for (int y = minYTile; y <= maxYTile; y++)
                    {
                        int chunkX = (x >> Map.ChunkSizeBits);
                        int chunkY = (y >> Map.ChunkSizeBits);
                        int chunkTileX = (x & Map.ChunkSizeModulo);
                        int chunkTileY = (y & Map.ChunkSizeModulo);
                        if (Map.InTileBounds(x, y))
                        {
                            if (Map.IsWall(chunkX, chunkY, chunkTileX, chunkTileY))
                            {
                                wallsToDeactivate.Remove(new Point(x, y));
                                ActivateWallBody(x, y);
                            }
                            else if (Map.IsObstacle(chunkX, chunkY, chunkTileX, chunkTileY))
                            {
                                obstaclesToDeactivate.Remove(new Point(x, y));
                                ActivateObstacleBody(x, y);
                            }
                        }
                    }
                foreach (Point point in wallsToDeactivate)
                    DeactivateWallBody(point.X, point.Y);
                foreach (Point point in obstaclesToDeactivate)
                    DeactivateObstacleBody(point.X, point.Y);
                Self.OldPosition = Self.Position;
            }
            for (int i = 0; i < Players.Length; i++)
                if (Players[i] != null)
                    Players[i].Update(gameTime);
            _syncTimer += gameTime.ElapsedGameTime.Ticks;
            _onUpdate?.Invoke(gameTime);
            Net.Update();
            float virtualWidthOver2 = (Game1.VirtualWidth / 2f);
            float virtualHeightOver2 = (Game1.VirtualHeight / 2f);
            Map.Bake((Camera.X - virtualWidthOver2), (Camera.Y - virtualHeightOver2), (Camera.X + virtualWidthOver2), (Camera.Y + virtualHeightOver2));
            base.Update(gameTime);
        }

        public static void UpdateServer(GameTime gameTime)
        {
            if (_syncTimer >= SyncRate)
            {
                _syncTimer -= SyncRate;
                for (int i = 0; i < Players.Length; i++)
                {
                    Player player1 = Players[i];
                    if ((player1 == null) || (player1 == Self) || (player1.Connection == null) || (Net.PlayerState[player1.ID] != Player.NetState.Playing))
                        continue;
                    NetOutgoingMessage msg = Net.Server.CreateMessage();
                    msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.PlayerSync);
                    for (int j = 0; j < Players.Length; j++)
                    {
                        if (j == i)
                            continue;
                        Player player2 = Players[j];
                        if (player2 == null)
                            continue;
                        msg.WriteRangedInteger(0, PlayerCountIndex, player2.ID);
                        msg.WriteRangedInteger(0, MapWidth, (int)player2.Position.X);
                        msg.WriteRangedInteger(0, MapHeight, (int)player2.Position.Y);
                        msg.WriteRangedInteger(Player.MinAngle, Player.MaxAngle, player2.PackedAngle);
                    }
                    Net.Server.SendMessage(msg, player1.Connection, NetDeliveryMethod.UnreliableSequenced, 1);
                }
            }
        }

        public static void UpdateClient(GameTime gameTime)
        {
            if (_syncTimer >= SyncRate)
            {
                _syncTimer -= SyncRate;
                NetOutgoingMessage msg = Net.Client.CreateMessage();
                msg.WriteRangedInteger(0, Net.ClientPacketCountIndex, (int)Net.ClientPacket.PlayerSync);
                msg.WriteRangedInteger(0, MapWidth, (int)Self.Position.X);
                msg.WriteRangedInteger(0, MapHeight, (int)Self.Position.Y);
                msg.WriteRangedInteger(Player.MinAngle, Player.MaxAngle, Self.PackedAngle);
                Net.Client.SendMessage(msg, NetDeliveryMethod.UnreliableSequenced, 1);
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            Map.Draw(spriteBatch, gameTime);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null, null, null, Camera.Transform);
            foreach (Player player in Players)
                if (player != null)
                    player.Draw(spriteBatch, gameTime);
            foreach (Bullet bullet in Bullets)
                bullet.Draw(spriteBatch, gameTime);
            spriteBatch.End();
            if (Game1.DebugMenu.Groups["D2D"].Items["Line to Grid"].SelectedOption?.Text == "On")
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.LinearClamp, null, null, null, Camera.Transform);
                //Line ray = new Line(Self.Position, new Vector2((float)(Self.Position.X + (Math.Cos(Angle) * 4095)), (float)(Self.Position.Y + (Math.Sin(Angle) * 4095))));
                Line ray = new Line(Self.Position, Camera.MousePosition);
                ray.Draw(spriteBatch, 1, Color.White, 0);
                foreach (Point tilePoint in Map.TilePointsAlongRayUntilSolid((int)ray.StartX, (int)ray.StartY, (int)ray.EndX, (int)ray.EndY))
                    spriteBatch.Draw(Game1.Pixel, new Rectangle((tilePoint.X * 32), (tilePoint.Y * 32), 32, 32), (Color.Lime * .4f));
                Point? firstSolidTileHitByRay = Map.FirstSolidTileHit((int)ray.StartX, (int)ray.StartY, (int)ray.EndX, (int)ray.EndY);
                if (firstSolidTileHitByRay.HasValue)
                {
                    Line top = new Line((firstSolidTileHitByRay.Value.X * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize), ((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize));
                    Line right = new Line(((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize), ((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize));
                    Line bottom = new Line(((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.X * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize));
                    Line left = new Line((firstSolidTileHitByRay.Value.X * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.X * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize));
                    if (Self.Position.Y < top.StartY)
                        top.Draw(spriteBatch, 2, Color.LimeGreen, 0);
                    if (Self.Position.X > right.StartX)
                        right.Draw(spriteBatch, 2, Color.LimeGreen, 0);
                    if (Self.Position.Y > bottom.StartY)
                        bottom.Draw(spriteBatch, 2, Color.LimeGreen, 0);
                    if (Self.Position.X < left.StartX)
                        left.Draw(spriteBatch, 2, Color.LimeGreen, 0);
                }
                spriteBatch.End();
            }
            if (Game1.DebugMenu.Groups["D2D"].Items["Player Info"].SelectedOption?.Text == "On")
            {
                spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, null);
                string text = "Players:";
                foreach (Player player in Players)
                    if (player != null)
                        text += string.Format("\n  ID: {0}, RandomSeed: {1}, Health: {2}, PackedAngle: {3}, Dead: {4}", player.ID, player.RandomSeed, player.Health, player.PackedAngle, player.Dead);
                spriteBatch.DrawString(Game1.Font, text, new Vector2((Game1.DebugDraw ? 217 : 5), 5), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
                spriteBatch.DrawString(Game1.Font, text, new Vector2((Game1.DebugDraw ? 216 : 4), 4), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
                spriteBatch.End();

                //_basicEffect.Projection = Camera.Projection;
                //_basicEffect.View = Camera.Transform;
                //_basicEffect.CurrentTechnique.Passes[0].Apply();
                //DrawSolidCircle(new Vector2(), 32, new Vector2(), Color.White);
                //DrawSolidPolygon(new Vector2(), Color.White, new Vector2(0, 0), new Vector2(32, 0), new Vector2(32, 32), new Vector2(0, 32));
            }
            //text += "\n\nGuns:";
            //foreach (GunStats gunStats in Guns.Values)
            //    text += string.Format("  AngleSpreadInitial: {0}, AngleSpreadWidenMax: {1}, AngleSpreadWidenRateIncreasePerShot: {2}, AngleSpreadWidenCooldownDecreasePerSecond: {3}", gunStats.AngleSpreadInitial, gunStats.AngleSpreadWidenMax, gunStats.AngleSpreadWidenRateIncreasePerShot, gunStats.AngleSpreadWidenCooldownDecreasePerSecond);
            if (Game1.DebugMenu.Groups["D2D"].Items["Physics"].SelectedOption?.Text == "On")
            {
                Matrix projection = Camera.SimProjection;
                Matrix view = Camera.SimTransform;
                //_debugView.RenderDebugData(ref projection, ref view);
                _debugView.BeginCustomDraw(projection, view);
                if (_debugView.Flags.HasFlag(DebugViewFlags.Shape))
                    foreach (Body b in World.BodyList)
                    {
                        if (!b.Enabled)
                            continue;
                        Transform xf;
                        b.GetTransform(out xf);
                        foreach (Fixture f in b.FixtureList)
                        {
                            if (b.BodyType == BodyType.Static)
                                _debugView.DrawShape(f, xf, _debugView.StaticShapeColor);
                            else if (b.BodyType == BodyType.Kinematic)
                                _debugView.DrawShape(f, xf, _debugView.KinematicShapeColor);
                            else if (b.Awake == false)
                                _debugView.DrawShape(f, xf, _debugView.SleepingShapeColor);
                            else
                                _debugView.DrawShape(f, xf, _debugView.DefaultShapeColor);
                        }
                    }
                if (_debugView.Flags.HasFlag(DebugViewFlags.ContactPoints))
                    foreach (ContactPoint point in _debugView.ContactPoints)
                    {
                        if (point.State == PointState.Add)
                            _debugView.DrawPoint(point.Position, 0.1f, new Color(0.3f, 0.95f, 0.3f));
                        else if (point.State == PointState.Persist)
                            _debugView.DrawPoint(point.Position, 0.1f, new Color(0.3f, 0.3f, 0.95f));
                    }
                _debugView.ContactPoints.Clear();
                _debugView.EndCustomDraw();
            }
            base.Draw(spriteBatch, gameTime);
        }

        public static Player GetPlayer(int id)
        {
            return Players[id];
        }

        public static Player GetPlayer(NetConnection connection)
        {
            if (PlayersByConnection.ContainsKey(connection))
                return PlayersByConnection[connection];
            return null;
        }

        internal static Player SetPlayer(int id, Player player)
        {
            Player existingPlayer;
            if ((existingPlayer = Players[id]) != null)
                if ((existingPlayer.Connection != null) && PlayersByConnection.ContainsKey(existingPlayer.Connection))
                        PlayersByConnection.Remove(existingPlayer.Connection);
            Players[id] = player;
            if (player.Connection != null)
            {
                if (PlayersByConnection.ContainsKey(player.Connection))
                    PlayersByConnection.Remove(player.Connection);
                PlayersByConnection.Add(player.Connection, Players[id]);
            }
            return Players[id];
        }

        internal static Player AddPlayer(NetConnection connection)
        {
            for (int i = 0; i < Players.Length; i++)
                if (Players[i] == null)
                {
                    Players[i] = new Player(i, connection);
                    if (connection != null)
                    {
                        if (PlayersByConnection.ContainsKey(connection))
                            PlayersByConnection.Remove(connection);
                        PlayersByConnection.Add(connection, Players[i]);
                    }
                    Players[i].Random = new Random(Players[i].RandomSeed = Game1.Random.Next());
                    return Players[i];
                }
            return null;
        }

        internal static void RemovePlayer(int id)
        {
            Player player = Players[id];
            if ((player.Connection != null) && PlayersByConnection.ContainsKey(player.Connection))
                PlayersByConnection.Remove(player.Connection);
            Players[id] = null;
        }

        internal static void RemovePlayer(NetConnection connection)
        {
            if (connection == null)
                return;
            if (PlayersByConnection.ContainsKey(connection))
            {
                Player player = PlayersByConnection[connection];
                if (Players[player.ID] == player)
                    Players[player.ID] = null;
                PlayersByConnection.Remove(connection);
            }
            else
                for (int i = 0; i < Players.Length; i++)
                    if (Players[i].Connection == connection)
                        Players[i] = null;
        }

        public static bool PrimaryAttack(Player player, out float angleSpread)//, out int bulletStartX, out int bulletStartY)
        {
            //angleSpread = bulletStartX = bulletStartY = 0;
            angleSpread = 0;
            InventoryItem inventoryItem = player.Inventory[player.SelectedInventorySlot];
            if (inventoryItem.Type == InventoryItem.Types.Gun)
            {
                if (player == Self)
                {
                    if (inventoryItem.FireTimer > 0)
                        return false;
                    //if (inventoryItem.AmmoInClip <= 0)
                    //    return false;
                    inventoryItem.FireTimer = inventoryItem.GunStats.RoundsPerSecond;
                    if (inventoryItem.AngleSpread == inventoryItem.GunStats.AngleSpreadInitial)
                        Body.ApplyForce(new Vector2(((float)Math.Cos(player.Angle) * -inventoryItem.GunStats.KickPhysicalInitial), ((float)Math.Sin(player.Angle) * -inventoryItem.GunStats.KickPhysicalInitial)));
                    else
                        Body.ApplyForce(new Vector2(((float)Math.Cos(player.Angle) * -inventoryItem.GunStats.KickPhysicalLead), ((float)Math.Sin(player.Angle) * -inventoryItem.GunStats.KickPhysicalLead)));
                }
                //angleSpreadPacked = (int)(MathHelper.Clamp(MathHelper.ToDegrees((float)inventoryItem.AngleSpread), 0, 90) * 10);
                //float angleSpread = MathHelper.ToRadians((float)(angleSpreadPacked / 10d));
                angleSpread = (float)inventoryItem.AngleSpread;
                float anglePerBullet = (angleSpread / inventoryItem.GunStats.BulletsPerShot);
                float maxSpreadPerBullet = (anglePerBullet - Player.AngleStep);
                inventoryItem.AmmoInClip--;
                inventoryItem.TotalAmmo--;
                float startAngle = (player.Angle - (angleSpread / 2));
                if (inventoryItem.AngleSpread == inventoryItem.GunStats.AngleSpreadInitial)
                    player.VisualKick = Math.Min(inventoryItem.GunStats.KickVisualMax, (player.VisualKick + inventoryItem.GunStats.KickVisualInitial));
                else
                    player.VisualKick = Math.Min(inventoryItem.GunStats.KickVisualMax, (player.VisualKick + inventoryItem.GunStats.KickVisualLead));
                inventoryItem.AngleSpread = Math.Min(inventoryItem.GunStats.AngleSpreadWidenMax, (angleSpread + inventoryItem.GunStats.AngleSpreadWidenRateIncreasePerShot));
                int bulletStartX = (int)player.Position.X;
                int bulletStartY = (int)player.Position.Y;
                Vector2 bulletStart = new Vector2(bulletStartX, bulletStartY);
                //NetOutgoingMessage msg = null;
                //if (Net.Server != null)
                //{
                //    msg = Net.Server.CreateMessage();
                //    msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.PrimaryAttack);
                //    msg.WriteRangedInteger(0, PlayerCountIndex, Self.ID);
                //}
                //else if (Net.Client != null)
                //{
                //    msg = Net.Client.CreateMessage();
                //    msg.WriteRangedInteger(0, Net.ClientPacketCountIndex, (int)Net.ServerPacket.PrimaryAttack);
                //}
                //if (msg != null)
                //{
                //    msg.WriteRangedInteger(0, MapWidth, intBulletStartX);
                //    msg.WriteRangedInteger(0, MapHeight, intBulletStartY);
                //}
                for (int i = 0; i < inventoryItem.GunStats.BulletsPerShot; i++)
                {
                    float bulletAngle = (startAngle + (i * anglePerBullet));
                    if (i == (inventoryItem.GunStats.BulletsPerShot - 1))
                        bulletAngle += (float)(anglePerBullet * player.Random.NextDouble());
                    else
                        bulletAngle += (float)(maxSpreadPerBullet * player.Random.NextDouble());
                    double cosBulletAngle = Math.Cos(bulletAngle);
                    double sinBulletAngle = Math.Sin(bulletAngle);
                    int intBulletEndX = (int)(bulletStart.X + (cosBulletAngle * inventoryItem.GunStats.RangePerBullet));
                    int intBulletEndY = (int)(bulletStart.Y + (sinBulletAngle * inventoryItem.GunStats.RangePerBullet));
                    Vector2 bulletEnd = new Vector2(intBulletEndX, intBulletEndY);
                    Line bulletLine = new Line(bulletStart, bulletEnd);
                    HashSet<Player> potentialPlayersHit = new HashSet<Player> { player };
                    List<BulletHitInfo> playersHit = new List<BulletHitInfo>(Players.Length - 1);
                    List<Point> tilePointsAlongBullet = Map.TilePointsAlongRayUntilSolid(bulletStartX, bulletStartY, intBulletEndX, intBulletEndY).ToList();
                    foreach (Point tilePoint in tilePointsAlongBullet)
                    {
                        ArrayList playersInTile = PlayerSpatialHash.Query(tilePoint.X, tilePoint.Y);
                        if (playersInTile != null)
                            foreach (Player player2 in playersInTile)
                                if (!potentialPlayersHit.Contains(player2))
                                {
                                    potentialPlayersHit.Add(player2);
                                    if (player2.Dead || (player2.Health < 1))
                                        continue;
                                    if (bulletLine.Intersects(player2.HeadMask))
                                    {
                                        playersHit.Add(new BulletHitInfo(player2, BulletHitInfo.HitTypes.Head));
                                        continue;
                                    }
                                    for (int j = 0; j < player2.ShoulderMasks.Length; j++)
                                        if (bulletLine.Intersects(player2.ShoulderMasks[j]))
                                        {
                                            playersHit.Add(new BulletHitInfo(player2, BulletHitInfo.HitTypes.Shoulder));
                                            break;
                                        }
                                }
                    }
                    Point? firstSolidTileHitByRay = ((tilePointsAlongBullet.Count > 0) ? tilePointsAlongBullet[tilePointsAlongBullet.Count - 1] : (Point?)null);
                    if (firstSolidTileHitByRay.HasValue)
                    {
                        Line top = new Line((firstSolidTileHitByRay.Value.X * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize), ((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize));
                        Line right = new Line(((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize), ((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize));
                        Line bottom = new Line(((firstSolidTileHitByRay.Value.X + 1) * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.X * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize));
                        Line left = new Line((firstSolidTileHitByRay.Value.X * Map.TileSize), ((firstSolidTileHitByRay.Value.Y + 1) * Map.TileSize), (firstSolidTileHitByRay.Value.X * Map.TileSize), (firstSolidTileHitByRay.Value.Y * Map.TileSize));
                        Vector2 intersection = Vector2.Zero;
                        if ((bulletStart.Y < top.StartY) && bulletLine.Intersects(top, out intersection))
                        {
                            bulletEnd.X = intBulletEndX = (int)intersection.X;
                            bulletEnd.Y = intBulletEndY = (int)intersection.Y;
                            bulletLine = new Line(bulletStart, bulletEnd);
                        }
                        if ((bulletStart.X > right.StartX) && bulletLine.Intersects(right, out intersection))
                        {
                            bulletEnd.X = intBulletEndX = (int)intersection.X;
                            bulletEnd.Y = intBulletEndY = (int)intersection.Y;
                            bulletLine = new Line(bulletStart, bulletEnd);
                        }
                        if ((bulletStart.Y > bottom.StartY) && bulletLine.Intersects(bottom, out intersection))
                        {
                            bulletEnd.X = intBulletEndX = (int)intersection.X;
                            bulletEnd.Y = intBulletEndY = (int)intersection.Y;
                            bulletLine = new Line(bulletStart, bulletEnd);
                        }
                        if ((bulletStart.X < left.StartX) && bulletLine.Intersects(left, out intersection))
                        {
                            bulletEnd.X = intBulletEndX = (int)intersection.X;
                            bulletEnd.Y = intBulletEndY = (int)intersection.Y;
                            bulletLine = new Line(bulletStart, bulletEnd);
                        }
                    }
                    float bulletDamage = inventoryItem.GunStats.MaxDamagePerBullet;
                    float bulletDistTraversed = Vector2.Distance(bulletStart, bulletEnd);
                    for (int j = 0; j < playersHit.Count; j++)
                    {
                        float distToBulletStartJ = Vector2.Distance(Players[playersHit[j].Victim.ID].Position, bulletStart);
                        for (int k = (j + 1); k < playersHit.Count; k++)
                        {
                            float distToBulletStartK = Vector2.Distance(Players[playersHit[k].Victim.ID].Position, bulletStart);
                            if (distToBulletStartK < distToBulletStartJ)
                            {
                                BulletHitInfo playerHitJ = playersHit[j];
                                playersHit[j] = playersHit[k];
                                playersHit[k] = playerHitJ;
                                distToBulletStartJ = distToBulletStartK;
                            }
                        }
                        BulletHitInfo bulletHitInfo = playersHit[j];
                        Player player2 = Players[bulletHitInfo.Victim.ID];
                        bulletDamage -= (((distToBulletStartJ - bulletDistTraversed) / PixelsPerMeter) * inventoryItem.GunStats.DamageKnockoffPerMeter);
                        bulletDistTraversed = distToBulletStartJ;
                        if (bulletDamage <= 0)
                        {
                            if (bulletDamage < 0)
                                bulletDistTraversed -= (distToBulletStartJ - ((Math.Abs(bulletDamage) / inventoryItem.GunStats.DamageKnockoffPerMeter) * PixelsPerMeter));
                            break;
                        }
                        if (player == Self)
                        {
                            int damage = (int)MathHelper.Clamp(bulletDamage, 1, player2.Health);
                            if (Net.Server != null)
                            {
                                NetOutgoingMessage msg = Net.Server.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.Damage);
                                msg.WriteRangedInteger(0, Net.DamageTypeBitSizeIndex, 0);
                                msg.WriteRangedInteger(0, PlayerCountIndex, Self.ID);
                                msg.WriteRangedInteger(0, PlayerCountIndex, player2.ID);
                                msg.WriteRangedInteger(1, player2.Health, damage);
                                msg.WriteRangedInteger(0, BulletHitInfo.HitTypesCountIndex, (int)bulletHitInfo.HitType);
                                Net.Server.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                            else if (Net.Client != null)
                            {
                                NetOutgoingMessage msg = Net.Client.CreateMessage();
                                msg.WriteRangedInteger(0, Net.ClientPacketCountIndex, (int)Net.ClientPacket.Damage);
                                msg.WriteRangedInteger(0, Net.DamageTypeBitSizeIndex, 0);
                                msg.WriteRangedInteger(0, PlayerCountIndex, player2.ID);
                                msg.WriteRangedInteger(1, player2.Health, damage);
                                msg.WriteRangedInteger(0, BulletHitInfo.HitTypesCountIndex, (int)bulletHitInfo.HitType);
                                Net.Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
                            }
                            player2.TakeDamage(damage, Self, bulletHitInfo.HitType);
                        }
                        bulletDamage -= ((bulletHitInfo.HitType == BulletHitInfo.HitTypes.Head) ? inventoryItem.GunStats.DamageKnockoffPerPlayerHeadHit : inventoryItem.GunStats.DamageKnockoffPerPlayerShoulderHit);
                        if (bulletDamage <= 0)
                            break;
                    }
                    //int packedBulletAngle = Player.PackAngle(bulletAngle);
                    //bulletAngle = Player.UnpackAngle(packedBulletAngle);
                    bulletDistTraversed = (int)bulletDistTraversed;
                    intBulletEndX = (int)(bulletStart.X + (cosBulletAngle * bulletDistTraversed));
                    intBulletEndY = (int)(bulletStart.Y + (sinBulletAngle * bulletDistTraversed));
                    bulletEnd = new Vector2(intBulletEndX, intBulletEndY);
                    //if (msg != null)
                    //{
                    //    //msg.WriteRangedInteger(0, MapWidth, (int)bulletEnd.X);
                    //    //msg.WriteRangedInteger(0, MapHeight, (int)bulletEnd.Y);
                    //    msg.WriteRangedInteger(-Player.AnglesOver2, Player.AnglesOver2, packedBulletAngle);
                    //    msg.WriteRangedInteger(0, 3000, (int)bulletDistTraversed);
                    //}
                    Bullets.Add(new Bullet(bulletStart, bulletEnd, bulletAngle));
                }
                //if (Net.Server != null)
                //{
                //    List<NetConnection> connections = Net.GetPlayingConnections();
                //    if (connections.Count > 0)
                //        Net.Server.SendMessage(msg, connections, NetDeliveryMethod.ReliableOrdered, 0);
                //}
                //else if (Net.Client != null)
                //    Net.Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
            }
            return true;
        }

        public static void MakeServer()
        {
            _onUpdate -= UpdateClient;
            _onUpdate += UpdateServer;
        }

        public static void MakeClient()
        {
            _onUpdate -= UpdateServer;
            _onUpdate += UpdateClient;
        }

        private static void GenerateWallPool(int instances)
        {
            if (_wallPoolInactive != null)
            {
                for (int i = 0; i < _wallPoolInactive.Count; i++)
                    World.RemoveBody(_wallPoolInactive.Dequeue());
                _wallPoolInactive.Clear();
                _wallPoolActive.Clear();
            }
            else
            {
                _wallPoolInactive = new Queue<Body>(instances);
                _wallPoolActive = new Dictionary<Point, Body>();
            }
            Vertices tileVerts = PolygonUtils.CreateRectangle(Map.TileSizeSimOver2, Map.TileSizeSimOver2);
            for (int i = 0; i < instances; i++)
            {
                Body body = BodyFactory.CreateBody(World, new Vector2(-10));
                body.CollisionCategories = VelcroPhysics.Collision.Filtering.Category.Cat1;
                FixtureFactory.AttachPolygon(tileVerts, 1, body);
                _wallPoolInactive.Enqueue(body);
            }
        }

        private static void GenerateObstaclePool(int instances)
        {
            if (_obstaclePoolInactive != null)
            {
                for (int i = 0; i < _obstaclePoolInactive.Count; i++)
                    World.RemoveBody(_obstaclePoolInactive.Dequeue());
                _obstaclePoolInactive.Clear();
                _obstaclePoolActive.Clear();
            }
            else
            {
                _obstaclePoolInactive = new Queue<Body>(instances);
                _obstaclePoolActive = new Dictionary<Point, Body>();
            }
            Vertices tileVerts = PolygonUtils.CreateRectangle(Map.TileSizeSimOver2, Map.TileSizeSimOver2);
            for (int i = 0; i < instances; i++)
            {
                Body body = BodyFactory.CreateBody(World, new Vector2(-10));
                body.CollisionCategories = VelcroPhysics.Collision.Filtering.Category.Cat2;
                FixtureFactory.AttachPolygon(tileVerts, 1, body);
                _obstaclePoolInactive.Enqueue(body);
            }
        }

        public static Body ActivateWallBody(int tileX, int tileY)
        {
            Point point = new Point(tileX, tileY);
            if (_wallPoolActive.ContainsKey(point))
                return null;
            Body body = null;
            if (_wallPoolInactive.Count <= 0)
            {
                body = BodyFactory.CreateBody(World);
                body.CollisionCategories = VelcroPhysics.Collision.Filtering.Category.Cat1;
                Vertices tileVerts = PolygonUtils.CreateRectangle(Map.TileSizeSimOver2, Map.TileSizeSimOver2);
                FixtureFactory.AttachPolygon(tileVerts, 1, body);
            }
            else
                body = _wallPoolInactive.Dequeue();
            body.Position = new Vector2(ConvertUnits.ToSimUnits(tileX * Map.TileSize + Map.TileSizeOver2), ConvertUnits.ToSimUnits(tileY * Map.TileSize + Map.TileSizeOver2));
            body.Enabled = true;
            _wallPoolActive.Add(point, body);
            return body;
        }

        public static Body ActivateObstacleBody(int tileX, int tileY)
        {
            Point point = new Point(tileX, tileY);
            if (_obstaclePoolActive.ContainsKey(point))
                return null;
            Body body = null;
            if (_obstaclePoolInactive.Count <= 0)
            {
                body = BodyFactory.CreateBody(World);
                body.CollisionCategories = VelcroPhysics.Collision.Filtering.Category.Cat2;
                Vertices tileVerts = PolygonUtils.CreateRectangle(Map.TileSizeSimOver2, Map.TileSizeSimOver2);
                FixtureFactory.AttachPolygon(tileVerts, 1, body);
            }
            else
                body = _obstaclePoolInactive.Dequeue();
            body.Position = new Vector2(ConvertUnits.ToSimUnits(tileX * Map.TileSize + Map.TileSizeOver2), ConvertUnits.ToSimUnits(tileY * Map.TileSize + Map.TileSizeOver2));
            body.Enabled = true;
            _obstaclePoolActive.Add(point, body);
            return body;
        }

        public static void DeactivateWallBody(int tileX, int tileY)
        {
            Point point = new Point(tileX, tileY);
            if (!_wallPoolActive.ContainsKey(point))
                return;
            Body body = _wallPoolActive[point];
            body.Position = new Vector2(-10);
            body.Enabled = false;
            _wallPoolInactive.Enqueue(body);
            _wallPoolActive.Remove(point);
        }

        public static void DeactivateObstacleBody(int tileX, int tileY)
        {
            Point point = new Point(tileX, tileY);
            if (!_obstaclePoolActive.ContainsKey(point))
                return;
            Body body = _obstaclePoolActive[point];
            body.Position = new Vector2(-10);
            body.Enabled = false;
            _obstaclePoolInactive.Enqueue(body);
            _obstaclePoolActive.Remove(point);
        }

        public static void SetTile(int tileX, int tileY, int tileId)
        {
            Tile oldTile = Map.GetTile(tileX, tileY);
            Map.TileModes oldTileMode = ((oldTile != null) ? Map.TileMode[oldTile.Id] : Map.TileModes.FloorWOSound);
            Map.SetTile(tileX, tileY, tileId);
            Map.TileModes newTileMode = ((oldTile != null) ? Map.TileMode[Map.GetTile(tileX, tileY).Id] : Map.TileModes.FloorWOSound);
            if (Map.IsWall(newTileMode))
            {
                if (Map.IsObstacle(oldTileMode))
                {
                    DeactivateObstacleBody(tileX, tileY);
                    ActivateWallBody(tileX, tileY);
                }
                else if (!Map.IsWall(oldTileMode))
                    ActivateWallBody(tileX, tileY);
            }
            else if (Map.IsObstacle(newTileMode))
            {
                if (Map.IsWall(oldTileMode))
                {
                    DeactivateWallBody(tileX, tileY);
                    ActivateObstacleBody(tileX, tileY);
                }
                else if (!Map.IsObstacle(oldTileMode))
                    ActivateObstacleBody(tileX, tileY);
            }
            else
            {
                if (Map.IsWall(oldTileMode))
                    DeactivateWallBody(tileX, tileY);
                else if (Map.IsObstacle(oldTileMode))
                    DeactivateObstacleBody(tileX, tileY);
            }
        }

        public static void DrawSolidCircle(Vector2 center, float radius, Vector2 axis, Color color)
        {
            const double increment = (Math.PI * (2d / CircleSegments));
            double theta = 0;

            //Vector2 v0 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
            Vector2 v0 = new Vector2((center.X + radius), (center.Y + radius));
            theta += increment;

            VertexPositionColor[] vertices = new VertexPositionColor[1000 - (1000 % 3)];
            int verticeCount = 0;

            for (int i = 1; i < CircleSegments - 1; i++)
            {
                Vector2 v1 = center + radius * new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));
                Vector2 v2 = center + radius * new Vector2((float)Math.Cos(theta + increment), (float)Math.Sin(theta + increment));

                vertices[verticeCount].Position = new Vector3(v0, -0.1f);
                vertices[verticeCount++].Color = color;
                vertices[verticeCount].Position = new Vector3(v1, -0.1f);
                vertices[verticeCount++].Color = color;
                vertices[verticeCount].Position = new Vector3(v2, -0.1f);
                vertices[verticeCount++].Color = color;

                theta += increment;
            }

            int primitiveCount = (verticeCount / 3);
            Program.Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, primitiveCount);
            verticeCount = 0;
        }

        public static void DrawSolidPolygon(Color color, params Vector2[] points)
        {
            VertexPositionColor[] vertices = new VertexPositionColor[(points.Length * 3) - (points.Length % 3)];
            int verticeCount = 0;
            for (int i = 1; i < (points.Length - 1); i++)
            {
                vertices[verticeCount].Position = new Vector3(points[0], -0.1f);
                vertices[verticeCount++].Color = color;
                vertices[verticeCount].Position = new Vector3(points[i], -0.1f);
                vertices[verticeCount++].Color = color;
                vertices[verticeCount].Position = new Vector3(points[i + 1], -0.1f);
                vertices[verticeCount++].Color = color;
            }
            Program.Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, (points.Length - 2));
        }
    }
}