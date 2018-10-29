namespace Top_Down_Shooter
{
    public class Tile
    {
        public const int TextureSize = 32;

        public int Id { get; internal set; }

        internal Map.TileModes TileXMode;
        internal Map.TileModes TileYMode;
        internal Map.TileModes TileXYMode;
    }
}