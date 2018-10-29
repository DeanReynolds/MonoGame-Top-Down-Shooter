using Lidgren.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using VelcroPhysics.Utilities;

namespace Top_Down_Shooter
{
    public class Player
    {
        public const int BodyRadius = 12;
        public const int BitsPerAngle = 10;

        public static readonly int Angles = (int)Math.Pow(2, BitsPerAngle);
        public static readonly int AnglesOver2 = (Angles / 2);
        public static readonly int MinAngle = -AnglesOver2;
        public static readonly int MaxAngle = (AnglesOver2 - 1);
        public static readonly float AngleStep = MathHelper.ToRadians((float)(360d / Angles));

        public static int PackAngle(float angle) { return ((((int)Math.Round((MathHelper.ToDegrees(MathHelper.WrapAngle(angle)) / 360) * Angles) + AnglesOver2) % Angles) - AnglesOver2); }
        public static float UnpackAngle(int packedAngle) { return MathHelper.ToRadians((packedAngle / (float)Angles) * 360); }

        public readonly int ID;
        public readonly NetConnection Connection;

        public Teams Team { get; internal set; }
        public Vector2 Position { get; internal set; }
        public Vector2 OldPosition { get; internal set; }
        public float Angle { get; internal set; }
        public int PackedAngle { get; internal set; }
        public int Health { get; internal set; }
        public bool Dead { get; internal set; }
        public BulletHitInfo LastHitBy { get; internal set; }
        public int RandomSeed { get; internal set; }
        public Random Random { get; internal set; }
        public int RandomIndex { get; internal set; }
        public float VisualKick { get; internal set; }

        public Vector2 SoftPosition { get; private set; }
        public Texture2D BodyTexture { get; private set; }
        public Vector2 BodyOrigin { get; private set; }
        public InventoryItem[] Inventory { get; private set; }
        public byte SelectedInventorySlot { get; private set; }
        public Polygon HeadMask { get; private set; }
        public Polygon[] ShoulderMasks { get; private set; }

        public enum NetState { Disconnected, Connecting, Playing }
        public enum Teams { Separatist, SWAT }

        public Player() : this(0, null) { }
        public Player(int id) : this(id, null) { }
        public Player(int id, NetConnection connection)
        {
            ID = id;
            Connection = connection;
            BodyTexture = Program.Game.Content.Load<Texture2D>("Textures\\Separatist_M4");
            BodyOrigin = new Vector2(8, 12);
            Inventory = new InventoryItem[4];
            for (int i = 0; i < Inventory.Length; i++)
                Inventory[i] = new InventoryItem(InventoryItem.Types.Empty, new GunStats());
            Inventory[0] = new InventoryItem(InventoryItem.Types.Gun, Scenes.Game.Guns["M4"]);
            SelectedInventorySlot = 0;
            HeadMask = Polygon.CreateCross(10);
            ShoulderMasks = new Polygon[2];
            for (int i = 0; i < ShoulderMasks.Length; i++)
                ShoulderMasks[i] = Polygon.CreateCross(6);
            Dead = true;
        }

        public void Update(GameTime gameTime)
        {
            if (VisualKick > 0)
            {
                VisualKick = (float)Math.Max(0, (VisualKick - (gameTime.ElapsedGameTime.TotalSeconds / Inventory[SelectedInventorySlot].GunStats.KickVisualRecoverRate)));
                Position = new Vector2((int)Math.Round(MathHelper.Clamp((float)(Position.X - (Math.Cos(Angle) * VisualKick)), 0, Scenes.Game.MapWidth)), (int)Math.Round(MathHelper.Clamp((float)(Position.Y - (Math.Sin(Angle) * VisualKick)), 0, Scenes.Game.MapHeight)));
            }
            double invLastVisitTimeDif = (gameTime.TotalGameTime.TotalSeconds - Inventory[SelectedInventorySlot].LastVisitTotalElapsedTime);
            if (Inventory[SelectedInventorySlot].FireTimer <= 0)
                Inventory[SelectedInventorySlot].AngleSpread = Math.Max(Inventory[SelectedInventorySlot].GunStats.AngleSpreadInitial, (Inventory[SelectedInventorySlot].AngleSpread - (Inventory[SelectedInventorySlot].GunStats.AngleSpreadWidenCooldownDecreasePerSecond * invLastVisitTimeDif)));
            Inventory[SelectedInventorySlot].FireTimer = Math.Max(0, (Inventory[SelectedInventorySlot].FireTimer - invLastVisitTimeDif));
            Inventory[SelectedInventorySlot].LastVisitTotalElapsedTime = gameTime.TotalGameTime.TotalSeconds;
            SoftPosition = Vector2.Lerp(SoftPosition, Position, MathHelper.Clamp((float)(Vector2.Distance(Position, SoftPosition) * (gameTime.ElapsedGameTime.TotalSeconds * 3)), 0, 1));
            if ((Net.Server != null) && (Net.PlayerState[ID] == NetState.Playing) && Dead)
            {
                NetOutgoingMessage msg = Net.Server.CreateMessage();
                msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.Connection);
                msg.WriteRangedInteger(0, Net.ConnectionTypeBitSizeIndex, 3);
                msg.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, ID);
                Net.Server.SendToAll(msg, Connection, NetDeliveryMethod.ReliableOrdered, 0);
                Respawn();
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            new Line(SoftPosition, Position).Draw(spriteBatch, 1, Color.Blue, 0);
            spriteBatch.Draw(BodyTexture, Position, null, (Color.Blue * .4f), Angle, BodyOrigin, 1, SpriteEffects.None, 0);
            if (!Dead)
            {
                if (Game1.DebugMenu.Groups["D2D"].Items["Player Spatial Hash"].SelectedOption?.Text == "On")
                {
                    const int bodyRadius = 64;
                    int minXTile = (int)((Position.X - bodyRadius) / PlayerSpatialHash.Size);
                    int minYTile = (int)((Position.Y - bodyRadius) / PlayerSpatialHash.Size);
                    int maxXTile = (int)((Position.X + bodyRadius) / PlayerSpatialHash.Size);
                    int maxYTile = (int)((Position.Y + bodyRadius) / PlayerSpatialHash.Size);
                    for (int x = minXTile; x <= maxXTile; x++)
                        for (int y = minYTile; y <= maxYTile; y++)
                        {
                            ArrayList players = PlayerSpatialHash.Query(x, y);
                            if ((players != null) && players.Contains(this))
                                spriteBatch.Draw(Game1.Pixel, new Rectangle((x * PlayerSpatialHash.Size), (y * PlayerSpatialHash.Size), PlayerSpatialHash.Size, PlayerSpatialHash.Size), (Color.Orange * .4f));
                        }
                }
                spriteBatch.Draw(BodyTexture, SoftPosition, null, Color.White, Angle, BodyOrigin, 1, SpriteEffects.None, 0);
                if (Game1.DebugMenu.Groups["D2D"].Items["Player Hitcrosses"].SelectedOption?.Text == "On")
                {
                    HeadMask.Draw(spriteBatch, 1, Color.White, 0);
                    for (int i = 0; i < ShoulderMasks.Length; i++)
                        ShoulderMasks[i].Draw(spriteBatch, 1, Color.Silver, 0);
                }
                //spriteBatch.Draw(Game1.Pixel, new Rectangle((xTile * 32), (yTile * 32), 32, 32), (Color.Lime * .4f));
            }
        }

        public void TakeDamage(int damage, Player player, BulletHitInfo.HitTypes hitType)
        {
            Health -= damage;
            LastHitBy = new BulletHitInfo(player, hitType);
            if (this == Scenes.Game.Self)
            {
                if (Net.Server != null)
                {
                    NetOutgoingMessage msg = Net.Server.CreateMessage();
                    msg.WriteRangedInteger(0, Net.ServerPacketCountIndex, (int)Net.ServerPacket.Damage);
                    msg.WriteRangedInteger(0, Net.DamageTypeBitSizeIndex, 1);
                    msg.WriteRangedInteger(0, Scenes.Game.PlayerCountIndex, ID);
                    Net.Server.SendToAll(msg, NetDeliveryMethod.ReliableOrdered);
                }
                else if (Net.Client != null)
                {
                    NetOutgoingMessage msg = Net.Client.CreateMessage();
                    msg.WriteRangedInteger(0, Net.ClientPacketCountIndex, (int)Net.ClientPacket.Damage);
                    msg.WriteRangedInteger(0, Net.DamageTypeBitSizeIndex, 1);
                    Net.Client.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
                }
                Die();
            }
        }

        public void Die()
        {
            Dead = true;
        }

        public void Respawn()
        {
            SoftPosition = Position = Vector2.Zero;
            if (this == Scenes.Game.Self)
                Scenes.Game.Body.Position = ConvertUnits.ToSimUnits(Scenes.Game.Camera.Position = Position);
            Health = 1000;
            Dead = false;
        }

        public void UpdateHitboxes()
        {
            HeadMask.Position = Position;
            float maskOffsetAngle = (Angle - 1.57079632679f);
            float shoulderXVel = (float)(Math.Cos(maskOffsetAngle) * 8);
            float shoulderYVel = (float)(Math.Sin(maskOffsetAngle) * 8);
            ShoulderMasks[0].Position = new Vector2((Position.X + shoulderXVel), (Position.Y + shoulderYVel));
            ShoulderMasks[1].Position = new Vector2((Position.X - shoulderXVel), (Position.Y - shoulderYVel));
            ShoulderMasks[1].Angle = ShoulderMasks[0].Angle = HeadMask.Angle = -Angle;
        }

        public void SnapSoftPosition() { SoftPosition = Position; }
    }
}