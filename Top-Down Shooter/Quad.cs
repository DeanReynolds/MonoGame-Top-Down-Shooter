using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Top_Down_Shooter
{
    public class Quad
    {
        public VertexPositionTexture[] Vertices { get; private set; }
        public int[] Indeces { get; private set; }

        public Quad(Map map, int x, int y, int tileId)
        {
            Vertices = new VertexPositionTexture[4];
            Indeces = new int[6];
            Rectangle s = map.TileSource[tileId];
            Vector2 textureUpperLeft = new Vector2((s.X / (float)map.TileSheet.Width), (s.Y / (float)map.TileSheet.Height));
            Vector2 textureUpperRight = new Vector2(((s.X + s.Width) / (float)map.TileSheet.Width), (s.Y / (float)map.TileSheet.Height));
            Vector2 textureLowerLeft = new Vector2((s.X / (float)map.TileSheet.Width), ((s.Y + s.Height) / (float)map.TileSheet.Height));
            Vector2 textureLowerRight = new Vector2(((s.X + s.Width) / (float)map.TileSheet.Width), ((s.Y + s.Height) / (float)map.TileSheet.Height));
            int j = (x * map.TileSize);
            int k = -(y * map.TileSize);
            int n = ((x + 1) * map.TileSize);
            int m = -((y + 1) * map.TileSize);
            Vertices[0].Position = new Vector3(j, m, 0);
            Vertices[0].TextureCoordinate = textureLowerLeft;
            Vertices[1].Position = new Vector3(j, k, 0);
            Vertices[1].TextureCoordinate = textureUpperLeft;
            Vertices[2].Position = new Vector3(n, m, 0);
            Vertices[2].TextureCoordinate = textureLowerRight;
            Vertices[3].Position = new Vector3(n, k, 0);
            Vertices[3].TextureCoordinate = textureUpperRight;
            Indeces[0] = 0;
            Indeces[1] = 1;
            Indeces[2] = 2;
            Indeces[3] = 2;
            Indeces[4] = 1;
            Indeces[5] = 3;
        }
    }
}