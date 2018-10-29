using Microsoft.Xna.Framework;
using System.Collections;
using System.Collections.Generic;

namespace Top_Down_Shooter
{
    public static class PlayerSpatialHash
    {
        public const int Size = 32;

        private static Hashtable _hashtable;
        private static Dictionary<Player, PlayerInfo> _storedPlayers;

        static PlayerSpatialHash()
        {
            _hashtable = new Hashtable();
            _storedPlayers = new Dictionary<Player, PlayerInfo>();
        }

        public static bool Add(Player player)
        {
            if (_storedPlayers.ContainsKey(player))
                return false;
            PlayerInfo playerInfo = new PlayerInfo(player);
            int minXTile = (int)((player.Position.X - Player.BodyRadius) / Size);
            int minYTile = (int)((player.Position.Y - Player.BodyRadius) / Size);
            int maxXTile = (int)((player.Position.X + Player.BodyRadius) / Size);
            int maxYTile = (int)((player.Position.Y + Player.BodyRadius) / Size);
            for (int x = minXTile; x <= maxXTile; x++)
                for (int y = minYTile; y <= maxYTile; y++)
                {
                    int hash = ((17 * 23 + x.GetHashCode()) * 23 + y.GetHashCode());
                    if (_hashtable.Contains(hash))
                    {
                        ArrayList cell = (ArrayList)_hashtable[hash];
                        cell.Add(player);
                    }
                    else
                        _hashtable.Add(hash, new ArrayList() { player });
                }
            _storedPlayers.Add(player, playerInfo);
            return true;
        }

        public static bool Update(Player player)
        {
            if (!_storedPlayers.ContainsKey(player))
                return false;
            PlayerInfo playerInfo = _storedPlayers[player];
            int oldMinXTile = (int)((playerInfo.StoredPosition.X - Player.BodyRadius) / Size);
            int oldMinYTile = (int)((playerInfo.StoredPosition.Y - Player.BodyRadius) / Size);
            int oldMaxXTile = (int)((playerInfo.StoredPosition.X + Player.BodyRadius) / Size);
            int oldMaxYTile = (int)((playerInfo.StoredPosition.Y + Player.BodyRadius) / Size);
            int newMinXTile = (int)((player.Position.X - Player.BodyRadius) / Size);
            int newMinYTile = (int)((player.Position.Y - Player.BodyRadius) / Size);
            int newMaxXTile = (int)((player.Position.X + Player.BodyRadius) / Size);
            int newMaxYTile = (int)((player.Position.Y + Player.BodyRadius) / Size);
            for (int x = oldMinXTile; x <= oldMaxXTile; x++)
                for (int y = oldMinYTile; y <= oldMaxYTile; y++)
                {
                    if (((x >= newMinXTile) && (y >= newMinYTile) && (x <= newMaxXTile) && (y <= newMaxYTile)))
                        continue;
                    int hash = ((17 * 23 + x.GetHashCode()) * 23 + y.GetHashCode());
                    if (_hashtable.Contains(hash))
                    {
                        ArrayList cell = (ArrayList)_hashtable[hash];
                        if (cell.Contains(player))
                        {
                            if (cell.Count == 1)
                                _hashtable.Remove(hash);
                            else
                                cell.Remove(player);
                        }
                    }
                }
            for (int x = newMinXTile; x <= newMaxXTile; x++)
                for (int y = newMinYTile; y <= newMaxYTile; y++)
                {
                    int hash = ((17 * 23 + x.GetHashCode()) * 23 + y.GetHashCode());
                    if (_hashtable.Contains(hash))
                    {
                        ArrayList cell = (ArrayList)_hashtable[hash];
                        if (!cell.Contains(player))
                            cell.Add(player);
                    }
                    else
                        _hashtable.Add(hash, new ArrayList() { player });
                }
            playerInfo.StoredPosition = player.Position;
            return true;
        }

        public static bool Remove(Player player)
        {
            if (!_storedPlayers.ContainsKey(player))
                return false;
            PlayerInfo playerInfo = _storedPlayers[player];
            int minXTile = (int)((playerInfo.StoredPosition.X - Player.BodyRadius) / Size);
            int minYTile = (int)((playerInfo.StoredPosition.Y - Player.BodyRadius) / Size);
            int maxXTile = (int)((playerInfo.StoredPosition.X + Player.BodyRadius) / Size);
            int maxYTile = (int)((playerInfo.StoredPosition.Y + Player.BodyRadius) / Size);
            for (int x = minXTile; x <= maxXTile; x++)
                for (int y = minYTile; y <= maxYTile; y++)
                {
                    int hash = ((17 * 23 + x.GetHashCode()) * 23 + y.GetHashCode());
                    if (_hashtable.Contains(hash))
                    {
                        ArrayList cell = (ArrayList)_hashtable[hash];
                        if (cell.Contains(player))
                            if (cell.Count == 1)
                                _hashtable.Remove(hash);
                            else
                                cell.Remove(player);
                    }
                }
            _storedPlayers.Remove(player);
            return true;
        }

        public static ArrayList Query(int tileX, int tileY)
        {
            int hash = ((17 * 23 + tileX.GetHashCode()) * 23 + tileY.GetHashCode());
            if (_hashtable.Contains(hash))
                return (ArrayList)((ArrayList)_hashtable[hash]).Clone();
            return null;
        }

        public static void Clear()
        {
            _hashtable.Clear();
            _storedPlayers.Clear();
        }

        private class PlayerInfo
        {
            public Player Player;
            public Vector2 StoredPosition;

            public PlayerInfo(Player player)
            {
                Player = player;
                StoredPosition = player.Position;
            }
        }
    }
}