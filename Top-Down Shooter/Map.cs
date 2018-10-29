using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VelcroPhysics.Utilities;

namespace Top_Down_Shooter
{
    public class Map
    {
        public static readonly Point[] dir8 = new[]
        {
            new Point(-1, -1), new Point(0, -1), new Point(1, -1),
            new Point(-1, 0), new Point(1, 0),
            new Point(-1, 1), new Point(0, 1), new Point(1, 1)
        };

        public const int ChunkSizeBits = 3;
        public const int ChunkBufferX = 0;
        public const int ChunkTwoBufferX = (ChunkBufferX * 2);
        public const int ChunkBufferY = 0;
        public const int ChunkTwoBufferY = (ChunkBufferY * 2);
        public const int ChunkUnloadBuffer = 1;
        public const int TwoChunkUnloadBuffer = (ChunkUnloadBuffer * 2);

        public static readonly int ChunkSize = (int)Math.Pow(2, ChunkSizeBits);
        public static readonly int ChunkSizeModulo = (ChunkSize - 1);

        public readonly Chunk[,] Chunks;
        public readonly int ChunksWidth;
        public readonly int ChunksHeight;
        public readonly int ChunksLastIndexX;
        public readonly int ChunksLastIndexY;
        public readonly int TilesWidth;
        public readonly int TilesHeight;
        public readonly int TilesWidthIndex;
        public readonly int TilesHeightIndex;
        public readonly int ChunkTextureSize;
        public readonly int ChunkBufferXTextureSize;
        public readonly int ChunkBufferYTextureSize;

        public RenderTarget2D TilesTexture { get; private set; }
        public RenderTarget2D ShadowsTexture { get; private set; }
        public int BakedChunksWidth { get; private set; }
        public int BakedChunksHeight { get; private set; }
        public int RawChunksMinX { get; private set; }
        public int RawChunksMaxX { get; private set; }
        public int RawChunksMinY { get; private set; }
        public int RawChunksMaxY { get; private set; }
        public int ChunksMinX { get; private set; }
        public int ChunksMaxX { get; private set; }
        public int ChunksMinY { get; private set; }
        public int ChunksMaxY { get; private set; }
        public float CameraX { get; private set; }
        public float CameraY { get; private set; }
        public int TileSize { get; private set; }
        public float TileSizeOver2 { get; private set; }
        public float TileSizeSim { get; private set; }
        public float TileSizeSimOver2 { get; private set; }
        public int TilesetCount { get; private set; }
        public int TilesetCountIndex { get; private set; }
        public TileModes[] TileMode { get; private set; }
        public Texture2D TileSheet
        {
            get { return _tileSheet; }
            private set
            {
                _tileSheet = value;
                if (_tileSheet != null)
                {
                    const int tileTextureSize = 32;
                    int widthTiles = (_tileSheet.Width / tileTextureSize);
                    int heightTiles = (_tileSheet.Height / tileTextureSize);
                    int tilesPerSheet = (widthTiles * heightTiles);
                    TileSource = new Rectangle[tilesPerSheet];
                    TileMode = new TileModes[tilesPerSheet];
                    for (int i = 0; i < TileSource.Length; i++)
                        TileSource[i] = new Rectangle(((i % widthTiles) * tileTextureSize), ((i / widthTiles) * tileTextureSize), tileTextureSize, tileTextureSize);
                    TilesetCountIndex = ((TilesetCount = tilesPerSheet) - 1);
                }
            }
        }
        public Rectangle[] TileSource { get; private set; }

        internal BasicEffect quadEffect;
        internal Quad quad;
        internal VertexPositionTexture[] _tileVertices;
        internal int[] _tileIndices;
        internal int _tileVerticeCount;
        internal int _tileIndiceCount;
        internal bool ShadowsNeedRebake;

        internal int ShadowVerticeCount { get; private set; }
        internal float ShadowCosWall { get; private set; }
        internal float ShadowSinWall { get; private set; }
        internal float ShadowCosObstacle { get; private set; }
        internal float ShadowSinObstacle { get; private set; }

        internal readonly Rectangle[,] TileDestination;
        internal readonly Point[,] TilePoint;
        internal readonly Point[,] ChunkPoint;
        internal readonly VertexPositionColor[] ShadowVertices;

        private int _oldRawChunksMinX;
        private int _oldRawChunksMaxX;
        private int _oldRawChunksMinY;
        private int _oldRawChunksMaxY;
        private HashSet<RenderTarget2D> _chunkTexturesActive;
        private Queue<RenderTarget2D> _chunkTexturesInactive;
        private Vector2 _drawOffset;
        private Texture2D _tileSheet;
        private float _shadowAngle;
        private double _shadowCos;
        private double _shadowSin;
        private float _shadowDistWall = 20;
        private float _shadowDistObstacle = 10;

        public enum TileModes { FloorWOSound = 0, Wall = 1, Obstacle = 2, WallWOShadow = 3, ObstacleWOShadow = 4, WallRAFloor = 5, FloorDirt = 10, FloorSnow = 11, FloorConcrete = 12, FloorTile = 13, FloorWade = 14, FloorMetal = 15, FloorWood = 16, DeadlyNormal = 50, DeadlyToxic = 51, DeadlyExplosion = 52, DeadlyAbyss = 53 }

        public Map(int tileSize, Texture2D tileSheet, int tilesWidth, int tilesHeight)
        {
            TileSizeOver2 = ((TileSize = tileSize) / 2f);
            ChunkTextureSize = (TileSize * ChunkSize);
            GenerateChunkTextures(58);
            ChunkBufferXTextureSize = (ChunkBufferX * ChunkTextureSize);
            ChunkBufferYTextureSize = (ChunkBufferY * ChunkTextureSize);
            TileSizeSimOver2 = ((TileSizeSim = ConvertUnits.ToSimUnits(TileSize)) / 2f);
            TileSheet = tileSheet;
            if ((tilesWidth <= 0) || (tilesHeight <= 0) || (tilesWidth > ushort.MaxValue) || (tilesHeight > ushort.MaxValue))
                throw new ArgumentOutOfRangeException(string.Format("World width and height must be between 0 and {0}", (ushort.MaxValue + 1)));
            int chunksWidth;
            if ((tilesWidth >= ChunkSize) && ((tilesWidth % ChunkSize) == 0))
                chunksWidth = (tilesWidth >> ChunkSizeBits);
            else
                chunksWidth = ((tilesWidth >> ChunkSizeBits) + 1);
            TilesWidthIndex = ((TilesWidth = (chunksWidth * ChunkSize)) - 1);
            int chunksHeight;
            if ((tilesHeight >= ChunkSize) && ((tilesHeight % ChunkSize) == 0))
                chunksHeight = (tilesHeight >> ChunkSizeBits);
            else
                chunksHeight = ((tilesHeight >> ChunkSizeBits) + 1);
            TilesHeightIndex = ((TilesHeight = (chunksHeight * ChunkSize)) - 1);
            Chunks = new Chunk[(ChunksWidth = chunksWidth), (ChunksHeight = chunksHeight)];
            ChunksLastIndexX = (ChunksWidth - 1);
            ChunksLastIndexY = (ChunksHeight - 1);
            TilePoint = new Point[TilesWidth, TilesHeight];
            ChunkPoint = new Point[ChunksWidth, ChunksHeight];
            for (int x = 0; x < ChunksWidth; x++)
                for (int y = 0; y < ChunksHeight; y++)
                {
                    Chunks[x, y] = new Chunk(this, x, y);
                    ChunkPoint[x, y] = new Point(x, y);
                    for (int j = 0; j < ChunkSize; j++)
                        for (int k = 0; k < ChunkSize; k++)
                            TilePoint[((x * ChunkSize) + j), ((y * ChunkSize) + k)] = new Point(((x * ChunkSize) + j), ((y * ChunkSize) + k));
                }
            TileDestination = new Rectangle[ChunkSize, ChunkSize];
            for (int x = 0; x < ChunkSize; x++)
                for (int y = 0; y < ChunkSize; y++)
                    TileDestination[x, y] = new Rectangle((x * TileSize), (y * TileSize), TileSize, TileSize);
            BakedChunksWidth = ((int)Math.Ceiling((Game1.VirtualWidth / (float)TileSize) / ChunkSize) + 1 + ChunkTwoBufferX);
            BakedChunksHeight = ((int)Math.Ceiling((Game1.VirtualHeight / (float)TileSize) / ChunkSize) + 1 + ChunkTwoBufferY);
            int textureWidth = (BakedChunksWidth * ChunkTextureSize);
            int textureHeight = (BakedChunksHeight * ChunkTextureSize);
            TilesTexture = new RenderTarget2D(Program.Game.GraphicsDevice, textureWidth, textureHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            TilesTexture.ContentLost += Texture_ContentLost;
            ShadowsTexture = new RenderTarget2D(Program.Game.GraphicsDevice, Game1.Viewport.Width, Game1.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            ShadowsTexture.ContentLost += Texture_ContentLost;
            ShadowVertices = new VertexPositionColor[99999];

            quadEffect = new BasicEffect(Program.Game.GraphicsDevice);
            quadEffect.TextureEnabled = true;
            quadEffect.Texture = TileSheet;
            quad = new Quad(this,0,0,0);
            _tileVertices = new VertexPositionTexture[99999];
            _tileIndices = new int[99999];
        }

        private void Texture_ContentLost(object sender, EventArgs e)
        {
            Bake();
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (ShadowsNeedRebake)
            {
                if (ShadowVerticeCount >= 3)
                {
                    Program.Game.GraphicsDevice.SetRenderTarget(ShadowsTexture);
                    Program.Game.GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;
                    Program.Game.GraphicsDevice.Clear(Color.TransparentBlack);
                    Scenes.Game._basicEffect.Projection = Scenes.Game.Camera.Projection;
                    Scenes.Game._basicEffect.View = Scenes.Game.Camera.Transform;
                    Scenes.Game._basicEffect.CurrentTechnique.Passes[0].Apply();
                    Program.Game.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, ShadowVertices, 0, (ShadowVerticeCount / 3));
                    Program.Game.GraphicsDevice.SetRenderTarget(null);
                }
                ShadowsNeedRebake = false;
            }
            Program.Game.GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, Scenes.Game.Camera.Transform);
            spriteBatch.Draw(TilesTexture, _drawOffset, null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.AnisotropicClamp, null, null, null, null);
            spriteBatch.Draw(ShadowsTexture, Vector2.Zero, null, (Color.White * .5f), 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            spriteBatch.End();

            //Program.Game.GraphicsDevice.Textures[0] = TileSheet;
            //Program.Game.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
            //quadEffect.Projection = Matrix.CreateOrthographic(960, 540, 0, 1);
            //quadEffect.View = (Matrix.CreateTranslation(-Scenes.Game.Camera.Position.X, Scenes.Game.Camera.Position.Y, 0) * Matrix.CreateScale(1, 1, 1));
            //quadEffect.World = Matrix.Identity;
            //quadEffect.CurrentTechnique.Passes[0].Apply();
            ////_tileVerticeCount = 0;
            ////_tileIndeceCount = 0;
            ////for (int x = (int)((Scenes.Game.Camera.X - (Game1.VirtualWidth / 2f)) / TileSize); x <= (int)((Scenes.Game.Camera.X + (Game1.VirtualWidth / 2f)) / TileSize); x++)
            ////{
            ////    for (int y = (int)((Scenes.Game.Camera.Y - (Game1.VirtualHeight / 2f)) / TileSize); y <= (int)((Scenes.Game.Camera.Y + (Game1.VirtualHeight / 2f)) / TileSize); y++)
            ////    {
            ////        if (InTileBounds(x, y))
            ////        {
            ////            Tile t = GetTile(x, y);
            ////            int tId = ((t != null) ? t.Id : 0);
            ////            if (tId > 0)
            ////            {
            ////                quad = new Quad(this, x, y, tId);
            ////                //Program.Game.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, quad.Vertices, 0, 4, quad.Indeces, 0, 2);
            ////                int startIndex = _tileVerticeCount;
            ////                for (int i = 0; i < quad.Vertices.Length; i++)
            ////                    _tileVertices[_tileVerticeCount++] = quad.Vertices[i];
            ////                for (int i = 0; i < quad.Indeces.Length; i++)
            ////                    _tileIndeces[_tileIndeceCount++] = (startIndex + quad.Indeces[i]);
            ////            }
            ////        }
            ////    }
            ////}
            //if (_tileVerticeCount > 0)
            //Program.Game.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _tileVertices, 0, _tileVerticeCount, _tileIndices, 0, (_tileIndiceCount / 3));
        }

        public bool InChunkBounds(int chunkX, int chunkY) { return ((chunkX >= 0) && (chunkY >= 0) && (chunkX < ChunksWidth) && (chunkY < ChunksHeight)); }

        public bool InTileBounds(int tileX, int tileY) { return ((tileX >= 0) && (tileY >= 0) && (tileX < TilesWidth) && (tileY < TilesHeight)); }

        private void GenerateChunkTextures(int capacity)
        {
            if (_chunkTexturesInactive == null)
            {
                _chunkTexturesInactive = new Queue<RenderTarget2D>(capacity);
                _chunkTexturesActive = new HashSet<RenderTarget2D>();
            }
            else
            {
                _chunkTexturesInactive.Clear();
                _chunkTexturesActive.Clear();
            }
            for (int i = 0; i < _chunkTexturesInactive.Count; i++)
                _chunkTexturesInactive.Enqueue(new RenderTarget2D(Program.Game.GraphicsDevice, ChunkTextureSize, ChunkTextureSize, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents));
        }

        private RenderTarget2D ActivateChunkTexture()
        {
            if (_chunkTexturesInactive.Count <= 0)
            {
                RenderTarget2D rt = new RenderTarget2D(Program.Game.GraphicsDevice, ChunkTextureSize, ChunkTextureSize, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
                _chunkTexturesActive.Add(rt);
                return rt;
            }
            else
            {
                RenderTarget2D rt = _chunkTexturesInactive.Dequeue();
                _chunkTexturesActive.Add(rt);
                return rt;
            }
        }

        private void DeactivateChunkTexture(RenderTarget2D rt)
        {
            if (!_chunkTexturesActive.Contains(rt))
                return;
            _chunkTexturesActive.Remove(rt);
            _chunkTexturesInactive.Enqueue(rt);
        }

        public void Bake(float cameraMinX, float cameraMinY, float cameraMaxX, float cameraMaxY)
        {
            int minChunkX = (((int)(cameraMinX / TileSize) >> ChunkSizeBits) - ChunkBufferX);
            CameraX = cameraMinX;
            _drawOffset.X = (minChunkX * ChunkTextureSize);
            RawChunksMinX = minChunkX;
            RawChunksMaxX = (((int)(cameraMaxX / TileSize) >> ChunkSizeBits) + ChunkBufferX);
            ChunksMinX = Math.Max(0, RawChunksMinX);
            ChunksMaxX = Math.Min(ChunksLastIndexX, RawChunksMaxX);
            int minChunkY = ((int)(cameraMinY / TileSize) >> ChunkSizeBits);
            CameraY = cameraMinY;
            _drawOffset.Y = ((minChunkY * ChunkTextureSize) - ChunkBufferYTextureSize);
            RawChunksMinY = (minChunkY - ChunkBufferY);
            RawChunksMaxY = (((int)(cameraMaxY / TileSize) >> ChunkSizeBits) + ChunkBufferY);
            ChunksMinY = Math.Max(0, RawChunksMinY);
            ChunksMaxY = Math.Min(ChunksLastIndexY, RawChunksMaxY);
            if ((RawChunksMinX != _oldRawChunksMinX) || (RawChunksMaxX != _oldRawChunksMaxX) || (RawChunksMinY != _oldRawChunksMinY) || (RawChunksMaxY != _oldRawChunksMaxY))
            {
                HashSet<Point> chunksToUnload = new HashSet<Point>();
                _oldRawChunksMaxX += TwoChunkUnloadBuffer;
                _oldRawChunksMaxY += TwoChunkUnloadBuffer;
                for (int x = (_oldRawChunksMinX - TwoChunkUnloadBuffer); x <= _oldRawChunksMaxX; x++)
                    if ((x >= 0) && (x < ChunksWidth))
                        for (int y = (_oldRawChunksMinY - TwoChunkUnloadBuffer); y <= _oldRawChunksMaxY; y++)
                            if ((y >= 0) && (y < ChunksHeight))
                                if (((x < (RawChunksMinX - ChunkUnloadBuffer)) || (y < (RawChunksMinY - ChunkUnloadBuffer)) || (x > (RawChunksMaxX + ChunkUnloadBuffer)) || (y > (RawChunksMaxY + ChunkUnloadBuffer))) && Chunks[x, y].HasBaked)
                                    chunksToUnload.Add(ChunkPoint[x, y]);
                for (int x = ChunksMinX; x <= ChunksMaxX; x++)
                    for (int y = ChunksMinY; y <= ChunksMaxY; y++)
                        if (!Chunks[x, y].HasBaked)
                        {
                            for (int j = 0; j < ChunkSize; j++)
                                for (int k = 0; k < ChunkSize; k++)
                                {
                                    int tX = ((x * ChunkSize) + j);
                                    int tY = ((y * ChunkSize) + k);
                                    UpdateShadow(tX, tY);
                                }
                            Chunks[x, y].Bake(ActivateChunkTexture());
                        }
                foreach (Point point in chunksToUnload)
                {
                    DeactivateChunkTexture(Chunks[point.X, point.Y].Texture);
                    Chunks[point.X, point.Y].ReleaseTexture();
                }
                Bake();
                ShadowVerticeCount = 0;
                for (int x = 0; x < ChunksWidth; x++)
                    for (int y = 0; y < ChunksHeight; y++)
                        if (Chunks[x, y].HasBaked && (Chunks[x, y].ShadowVerticeCount > 0))
                        {
                            Array.Copy(Chunks[x, y].ShadowVertices, 0, ShadowVertices, ShadowVerticeCount, Chunks[x, y].ShadowVerticeCount);
                            ShadowVerticeCount += Chunks[x, y].ShadowVerticeCount;
                        }
                ShadowsNeedRebake = true;
                _oldRawChunksMinX = RawChunksMinX;
                _oldRawChunksMaxX = RawChunksMaxX;
                _oldRawChunksMinY = RawChunksMinY;
                _oldRawChunksMaxY = RawChunksMaxY;
            }
        }

        private void Bake()
        {
            SpriteBatch spriteBatch = Program.Game.Services.GetService<SpriteBatch>();
            Program.Game.GraphicsDevice.SetRenderTarget(TilesTexture);
            Program.Game.GraphicsDevice.Clear(Color.TransparentBlack);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, null);
            int kReset = ((ChunksMinY - RawChunksMinY) * ChunkTextureSize);
            for (int x = ChunksMinX, j = ((ChunksMinX - RawChunksMinX) * ChunkTextureSize), k = kReset; x <= ChunksMaxX; x++, j += ChunkTextureSize, k = kReset)
                for (int y = ChunksMinY; y <= ChunksMaxY; y++, k += ChunkTextureSize)
                    spriteBatch.Draw(Chunks[x, y].Texture, new Rectangle(j, k, ChunkTextureSize, ChunkTextureSize), Color.White);
            spriteBatch.End();
            //_tileVerticeCount = 0;
            //_tileIndiceCount = 0;
            //for (int x = RawChunksMinX; x <= RawChunksMaxX; x++)
            //    if ((x >= 0) && (x < ChunksWidth))
            //        for (int y = RawChunksMinY; y <= RawChunksMaxY; y++)
            //            if ((y >= 0) && (y < ChunksHeight))
            //            {
            //                Array.Copy(Chunks[x, y].TileVertices, 0, _tileVertices, _tileVerticeCount, Chunks[x, y].TileVerticeCount);
            //                for (int i = 0, indice = 0; i < Chunks[x, y].TileIndiceCount; i++)
            //                    _tileIndices[_tileIndiceCount++] = (_tileVerticeCount + Chunks[x, y].TileIndices[indice++]);
            //                _tileVerticeCount += Chunks[x, y].TileVerticeCount;
            //                _tileIndiceCount += Chunks[x, y].TileIndiceCount;
            //            }
        }

        public Tile GetTile(int tileX, int tileY)
        {
            int chunkX = (tileX >> ChunkSizeBits);
            int chunkY = (tileY >> ChunkSizeBits);
            int chunkTileX = (tileX & ChunkSizeModulo);
            int chunkTileY = (tileY & ChunkSizeModulo);
            return GetTile(chunkX, chunkY, chunkTileX, chunkTileY);
        }

        public Tile GetTile(int chunkX, int chunkY, int chunkTileX, int chunkTileY) { return Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY]; }

        public IEnumerable<Point> TilePointsAlongRay(int x1, int y1, int x2, int y2)
        {
            Point curVoxel = new Point((x1 / TileSize), (y1 / TileSize));
            int xD = (x2 - x1);
            int stepX = Math.Sign(xD);
            double tDeltaX = ((xD != 0) ? ((TileSize / (double)xD) * stepX) : 1);
            double fX = ((double)x1 / TileSize);
            if (stepX > 0)
                fX = (1 - (fX - Math.Floor(fX)));
            else
                fX = (fX - Math.Floor(fX));
            double tMaxX = ((xD != 0) ? (tDeltaX * fX) : 1);
            int yD = (y2 - y1);
            int stepY = Math.Sign(yD);
            double tDeltaY = ((yD != 0) ? ((TileSize / (double)yD) * stepY) : 1);
            double fY = ((double)y1 / TileSize);
            if (stepY > 0)
                fY = (1 - (fY - Math.Floor(fY)));
            else
                fY = (fY - Math.Floor(fY));
            double tMaxY = ((xD != 0) ? (tDeltaY * fY) : 1);
            while ((tMaxX < 1) || (tMaxY < 1))
            {
                if (tMaxX < tMaxY)
                {
                    curVoxel.X += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    curVoxel.Y += stepY;
                    tMaxY += tDeltaY;
                }
                yield return curVoxel;
            }
        }

        public IEnumerable<Point> TilePointsAlongRayUntilSolid(int x1, int y1, int x2, int y2)
        {
            Point curVoxel = new Point((x1 / TileSize), (y1 / TileSize));
            int xD = (x2 - x1);
            int stepX = Math.Sign(xD);
            double tDeltaX = ((xD != 0) ? ((TileSize / (double)xD) * stepX) : 1);
            double fX = ((double)x1 / TileSize);
            if (stepX > 0)
                fX = (1 - (fX - Math.Floor(fX)));
            else
                fX = (fX - Math.Floor(fX));
            double tMaxX = ((xD != 0) ? (tDeltaX * fX) : 1);
            int yD = (y2 - y1);
            int stepY = Math.Sign(yD);
            double tDeltaY = ((yD != 0) ? ((TileSize / (double)yD) * stepY) : 1);
            double fY = ((double)y1 / TileSize);
            if (stepY > 0)
                fY = (1 - (fY - Math.Floor(fY)));
            else
                fY = (fY - Math.Floor(fY));
            double tMaxY = ((xD != 0) ? (tDeltaY * fY) : 1);
            while ((tMaxX < 1) || (tMaxY < 1))
            {
                if (tMaxX < tMaxY)
                {
                    curVoxel.X += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    curVoxel.Y += stepY;
                    tMaxY += tDeltaY;
                }
                yield return curVoxel;
                if (InTileBounds(curVoxel.X, curVoxel.Y))
                {
                    Tile tile = GetTile(curVoxel.X, curVoxel.Y);
                    if ((tile != null) && IsWall(curVoxel.X, curVoxel.Y))
                        break;
                }
            }
        }

        public Point? FirstSolidTileHit(int x1, int y1, int x2, int y2)
        {
            Point curVoxel = new Point((x1 / TileSize), (y1 / TileSize));
            int xD = (x2 - x1);
            int stepX = Math.Sign(xD);
            double tDeltaX = ((xD != 0) ? ((TileSize / (double)xD) * stepX) : 1);
            double fX = ((double)x1 / TileSize);
            if (stepX > 0)
                fX = (1 - (fX - Math.Floor(fX)));
            else
                fX = (fX - Math.Floor(fX));
            double tMaxX = ((xD != 0) ? (tDeltaX * fX) : 1);
            int yD = (y2 - y1);
            int stepY = Math.Sign(yD);
            double tDeltaY = ((yD != 0) ? ((TileSize / (double)yD) * stepY) : 1);
            double fY = ((double)y1 / TileSize);
            if (stepY > 0)
                fY = (1 - (fY - Math.Floor(fY)));
            else
                fY = (fY - Math.Floor(fY));
            double tMaxY = ((xD != 0) ? (tDeltaY * fY) : 1);
            while ((tMaxX < 1) || (tMaxY < 1))
            {
                if (tMaxX < tMaxY)
                {
                    curVoxel.X += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    curVoxel.Y += stepY;
                    tMaxY += tDeltaY;
                }
                if (InTileBounds(curVoxel.X, curVoxel.Y))
                {
                    Tile tile = GetTile(curVoxel.X, curVoxel.Y);
                    if ((tile != null) && IsWall(curVoxel.X, curVoxel.Y))
                        return curVoxel;
                }
            }
            return null;
        }

        internal bool SetTile(int tileX, int tileY, int id)
        {
            int chunkX = (tileX >> ChunkSizeBits);
            int chunkY = (tileY >> ChunkSizeBits);
            int chunkTileX = (tileX & ChunkSizeModulo);
            int chunkTileY = (tileY & ChunkSizeModulo);
            return SetTile(tileX, tileY, chunkX, chunkY, chunkTileX, chunkTileY, id);
        }

        internal bool SetTile(int tileX, int tileY, int chunkX, int chunkY, int chunkTileX, int chunkTileY, int id)
        {
            if (Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] != null)
            {
                if (Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id != id)
                {
                    if (IsWallOrObstacle(TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id]) != IsWallOrObstacle(TileMode[id]))
                        ShadowsNeedRebake = true;
                    Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id = id;
                    HashSet<Point> chunksToRebake = new HashSet<Point>();
                    for (int x = -1; x < 2; x++)
                        for (int y = -1; y < 2; y++)
                        {
                            int j = (tileX + x);
                            int k = (tileY + y);
                            if (InTileBounds(j, k))
                            {
                                Point? chunkPoint = UpdateShadow(j, k);
                                if (chunkPoint.HasValue && Chunks[chunkX, chunkY].HasBaked)
                                    chunksToRebake.Add(chunkPoint.Value);
                            }
                        }
                    bool needsBake = false;
                    foreach (Point chunkPoint in chunksToRebake)
                    {
                        Chunks[chunkPoint.X, chunkPoint.Y].Bake();
                        needsBake = true;
                    }
                    if (Chunks[chunkX, chunkY].HasBaked && !chunksToRebake.Contains(new Point(chunkX, chunkY)))
                    {
                        Chunks[chunkX, chunkY].Bake();
                        needsBake = true;
                    }
                    if (needsBake)
                        Bake();
                    return true;
                }
            }
            else
            {
                Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] = new Tile() { Id = id };
                if (IsWallOrObstacle(TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id]))
                    ShadowsNeedRebake = true;
                HashSet<Point> chunksToRebake = new HashSet<Point>();
                for (int x = -1; x < 2; x++)
                    for (int y = -1; y < 2; y++)
                    {
                        int j = (tileX + x);
                        int k = (tileY + y);
                        if (InTileBounds(j, k))
                        {
                            Point? chunkPoint = UpdateShadow(j, k);
                            if (chunkPoint.HasValue && Chunks[chunkX, chunkY].HasBaked)
                                chunksToRebake.Add(chunkPoint.Value);
                        }
                    }
                bool needsBake = false;
                foreach (Point chunkPoint in chunksToRebake)
                {
                    Chunks[chunkPoint.X, chunkPoint.Y].Bake();
                    needsBake = true;
                }
                if (Chunks[chunkX, chunkY].HasBaked && !chunksToRebake.Contains(new Point(chunkX, chunkY)))
                {
                    Chunks[chunkX, chunkY].Bake();
                    needsBake = true;
                }
                if (needsBake)
                    Bake();
                return true;
            }
            return false;
        }

        public bool RemoveTile(int tileX, int tileY)
        {
            int chunkX = (tileX >> ChunkSizeBits);
            int chunkY = (tileY >> ChunkSizeBits);
            int chunkTileX = (tileX & ChunkSizeModulo);
            int chunkTileY = (tileY & ChunkSizeModulo);
            if (Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] != null)
            {
                if (IsWallOrObstacle(TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id]))
                    ShadowsNeedRebake = true;
                Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] = null;
                HashSet<Point> chunksToRebake = new HashSet<Point>();
                for (int x = -1; x < 2; x++)
                    for (int y = -1; y < 2; y++)
                    {
                        int j = (tileX + x);
                        int k = (tileY + y);
                        if (InTileBounds(j, k))
                        {
                            Point? chunkPoint = UpdateShadow(j, k);
                            if (chunkPoint.HasValue && Chunks[chunkX, chunkY].HasBaked)
                                chunksToRebake.Add(chunkPoint.Value);
                        }
                    }
                bool needsBake = false;
                foreach (Point chunkPoint in chunksToRebake)
                {
                    Chunks[chunkPoint.X, chunkPoint.Y].Bake();
                    needsBake = true;
                }
                if (Chunks[chunkX, chunkY].HasBaked && !chunksToRebake.Contains(new Point(chunkX, chunkY)))
                {
                    Chunks[chunkX, chunkY].Bake();
                    needsBake = true;
                }
                if (needsBake)
                    Bake();
                return true;
            }
            return false;
        }

        public bool IsWall(int tileX, int tileY)
        {
            int chunkX = (tileX >> ChunkSizeBits);
            int chunkY = (tileY >> ChunkSizeBits);
            int chunkTileX = (tileX & ChunkSizeModulo);
            int chunkTileY = (tileY & ChunkSizeModulo);
            return IsWall(chunkX, chunkY, chunkTileX, chunkTileY);
        }

        internal bool IsWall(int chunkX, int chunkY, int chunkTileX, int chunkTileY)
        {
            if (Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] != null)
            {
                TileModes tileMode = TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id];
                return IsWall(tileMode);
            }
            return false;
        }

        internal bool IsWall(TileModes tileMode) { return ((tileMode == TileModes.Wall) || (tileMode == TileModes.WallWOShadow) || (tileMode == TileModes.WallRAFloor)); }

        public bool IsObstacle(int tileX, int tileY)
        {
            int chunkX = (tileX >> ChunkSizeBits);
            int chunkY = (tileY >> ChunkSizeBits);
            int chunkTileX = (tileX & ChunkSizeModulo);
            int chunkTileY = (tileY & ChunkSizeModulo);
            return IsObstacle(chunkX, chunkY, chunkTileX, chunkTileY);
        }

        internal bool IsObstacle(int chunkX, int chunkY, int chunkTileX, int chunkTileY)
        {
            if (Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] != null)
            {
                TileModes tileMode = TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id];
                return IsObstacle(tileMode);
            }
            return false;
        }

        internal bool IsObstacle(TileModes tileMode) { return ((tileMode == TileModes.Obstacle) || (tileMode == TileModes.ObstacleWOShadow)); }

        public bool IsWallOrObstacle(int tileX, int tileY)
        {
            int chunkX = (tileX >> ChunkSizeBits);
            int chunkY = (tileY >> ChunkSizeBits);
            int chunkTileX = (tileX & ChunkSizeModulo);
            int chunkTileY = (tileY & ChunkSizeModulo);
            return IsWallOrObstacle(chunkX, chunkY, chunkTileX, chunkTileY);
        }

        internal bool IsWallOrObstacle(int chunkX, int chunkY, int chunkTileX, int chunkTileY)
        {
            if (Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] != null)
            {
                TileModes tileMode = TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id];
                return IsWallOrObstacle(tileMode);
            }
            return false;
        }

        internal bool IsWallOrObstacle(TileModes tileMode) { return ((tileMode == TileModes.Wall) || (tileMode == TileModes.WallWOShadow) || (tileMode == TileModes.WallRAFloor) || (tileMode == TileModes.Obstacle) || (tileMode == TileModes.ObstacleWOShadow)); }

        private Point? UpdateShadow(int tileX, int tileY)
        {
            int chunkX = (tileX >> ChunkSizeBits);
            int chunkY = (tileY >> ChunkSizeBits);
            int chunkTileX = (tileX & ChunkSizeModulo);
            int chunkTileY = (tileY & ChunkSizeModulo);
            if (Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] == null)
                return null;

            int xF = ((ShadowCosWall > 0) ? (tileX + 1) : (tileX - 1));
            int yF = ((ShadowSinWall > 0) ? (tileY + 1) : (tileY - 1));
            Tile rightTile = (InTileBounds(xF, tileY) ? GetTile(xF, tileY) : null);
            Tile rightBelowTile = (InTileBounds(xF, yF) ? GetTile(xF, yF) : null);
            Tile belowTile = (InTileBounds(tileX, yF) ? GetTile(tileX, yF) : null);
            TileModes rightTileMode = ((rightTile != null) ? TileMode[rightTile.Id] : TileModes.FloorWOSound);
            TileModes rightBelowTileMode = ((rightBelowTile != null) ? TileMode[rightBelowTile.Id] : TileModes.FloorWOSound);
            TileModes belowTileMode = ((belowTile != null) ? TileMode[belowTile.Id] : TileModes.FloorWOSound);
            Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].TileXMode = rightTileMode;
            Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].TileYMode = belowTileMode;
            Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].TileXYMode = rightBelowTileMode;
            return null;

            //if (IsWall(TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id]))
            //{
            //    int xF = ((ShadowCosWall > 0) ? (tileX + 1) : (tileX - 1));
            //    int yF = ((ShadowSinWall > 0) ? (tileY + 1) : (tileY - 1));
            //    Tile rightTile = (InTileBounds(xF, tileY) ? GetTile(xF, tileY) : null);
            //    Tile rightBelowTile = (InTileBounds(xF, yF) ? GetTile(xF, yF) : null);
            //    Tile belowTile = (InTileBounds(tileX, yF) ? GetTile(tileX, yF) : null);
            //    TileModes rightTileMode = ((rightTile != null) ? TileMode[rightTile.Id] : TileModes.FloorWOSound);
            //    TileModes rightBelowTileMode = ((rightBelowTile != null) ? TileMode[rightBelowTile.Id] : TileModes.FloorWOSound);
            //    TileModes belowTileMode = ((belowTile != null) ? TileMode[belowTile.Id] : TileModes.FloorWOSound);
            //    int xN = (chunkTileX * TileSize);
            //    int xP1 = ((chunkTileX + 1) * TileSize);
            //    xF = ((ShadowCosWall > 0) ? xP1 : xN);
            //    int yN = (chunkTileY * TileSize);
            //    int yP1 = ((chunkTileY + 1) * TileSize);
            //    yF = ((ShadowSinWall > 0) ? yP1 : yN);
            //    if (!IsWall(rightTileMode) || !IsWall(belowTileMode))
            //        if (IsWall(rightTileMode))
            //        {
            //            if (IsObstacle(belowTileMode))
            //            {
            //                if (!IsWall(rightBelowTileMode))
            //                    AddYShadow(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                else
            //                    AddYShadow2(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            }
            //            else
            //            {
            //                if (IsWall(rightBelowTileMode))
            //                    AddYShadow2(xN, xP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //                else if (IsObstacle(rightBelowTileMode))
            //                {
            //                    AddYShadow2(xN, xP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //                    AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                }
            //                else
            //                    AddYShadow(xN, xP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //            }
            //        }
            //        else if (IsObstacle(rightTileMode))
            //        {
            //            if (!IsWall(belowTileMode))
            //            {
            //                if (!IsWall(rightBelowTileMode))
            //                {
            //                    if (IsObstacle(rightBelowTileMode))
            //                    {
            //                        if (IsObstacle(belowTileMode))
            //                        {
            //                            AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                            AddYShadow2(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                        }
            //                        else
            //                        {
            //                            AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                            AddYShadow2(xN, xP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                        }
            //                    }
            //                    else
            //                    {
            //                        if (IsObstacle(belowTileMode))
            //                        {
            //                            AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                            AddYShadow2(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //                        }
            //                        else
            //                        {
            //                            AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //                            AddYShadow2(xN, xP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //                        }
            //                    }
            //                }
            //                else
            //                    AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            }
            //            else if (!IsWall(rightBelowTileMode))
            //                AddXShadow(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            else
            //                AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //        }
            //        else if (IsWall(belowTileMode))
            //        {
            //            if (IsWall(rightBelowTileMode))
            //                AddXShadow2(yN, yP1, xF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //            else if (IsObstacle(rightBelowTileMode))
            //            {
            //                AddXShadow2(yN, yP1, xF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //                AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            }
            //            else
            //                AddXShadow(yN, yP1, xF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //        }
            //        else if (IsObstacle(belowTileMode))
            //        {
            //            AddXShadow2(yN, yP1, xF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //            AddYShadow2(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            if (IsObstacle(rightBelowTileMode))
            //                AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            else if (!IsWall(rightBelowTileMode))
            //                AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //        }
            //        else
            //        {
            //            AddXShadow2(yN, yP1, xF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //            AddYShadow2(xN, xP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //            if (IsObstacle(rightBelowTileMode))
            //                AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            else if (!IsWall(rightBelowTileMode))
            //                AddCornerShadow(xN, xP1, xF, yN, yP1, yF, ShadowCosWall, ShadowSinWall, chunkX, chunkY);
            //        }
            //}
            //else if (IsObstacle(TileMode[Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY].Id]))
            //{
            //    int xF = ((ShadowCosObstacle > 0) ? (tileX + 1) : (tileX - 1));
            //    int yF = ((ShadowSinObstacle > 0) ? (tileY + 1) : (tileY - 1));
            //    Tile rightTile = (InTileBounds(xF, tileY) ? GetTile(xF, tileY) : null);
            //    Tile rightBelowTile = (InTileBounds(xF, yF) ? GetTile(xF, yF) : null);
            //    Tile belowTile = (InTileBounds(tileX, yF) ? GetTile(tileX, yF) : null);
            //    TileModes rightTileMode = ((rightTile != null) ? TileMode[rightTile.Id] : TileModes.FloorWOSound);
            //    TileModes rightBelowTileMode = ((rightBelowTile != null) ? TileMode[rightBelowTile.Id] : TileModes.FloorWOSound);
            //    TileModes belowTileMode = ((belowTile != null) ? TileMode[belowTile.Id] : TileModes.FloorWOSound);
            //    int xN = (chunkTileX * TileSize);
            //    int xP1 = ((chunkTileX + 1) * TileSize);
            //    xF = ((ShadowCosObstacle > 0) ? xP1 : xN);
            //    int yN = (chunkTileY * TileSize);
            //    int yP1 = ((chunkTileY + 1) * TileSize);
            //    yF = ((ShadowSinObstacle > 0) ? yP1 : yN);
            //    if (!IsWallOrObstacle(rightTileMode) || !IsWallOrObstacle(belowTileMode))
            //        if (IsWallOrObstacle(rightTileMode))
            //        {
            //            if (IsWallOrObstacle(rightBelowTileMode))
            //                AddYShadow2(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            else
            //                AddYShadow(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //        }
            //        else if (IsWallOrObstacle(belowTileMode))
            //        {
            //            if (IsWallOrObstacle(rightBelowTileMode))
            //                AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            else
            //                AddXShadow(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //        }
            //        else if (IsWallOrObstacle(rightBelowTileMode))
            //        {
            //            AddXShadow2(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            AddYShadow2(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //        }
            //        else
            //        {
            //            AddXShadow(yN, yP1, xF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //            AddYShadow(xN, xP1, yF, ShadowCosObstacle, ShadowSinObstacle, chunkX, chunkY);
            //        }
            //}
            //return null;
        }

        internal void AddShadowTriangle(Vector2 p1, Vector2 p2, Vector2 p3, int chunkX, int chunkY)
        {
            int j = (chunkX * ChunkTextureSize);
            int k = (chunkY * ChunkTextureSize);
            Color color = Color.Black;
            ShadowVertices[ShadowVerticeCount].Position = new Vector3((j + p1.X), (k + p1.Y), -0.1f);
            ShadowVertices[ShadowVerticeCount++].Color = color;
            ShadowVertices[ShadowVerticeCount].Position = new Vector3((j + p2.X), (k + p2.Y), -0.1f);
            ShadowVertices[ShadowVerticeCount++].Color = color;
            ShadowVertices[ShadowVerticeCount].Position = new Vector3((j + p3.X), (k + p3.Y), -0.1f);
            ShadowVertices[ShadowVerticeCount++].Color = color;
        }

        internal void AddXShadow(int yN, int yP1, int xF, float cos, float sin, int chunkX, int chunkY)
        {
            float yPSin = (yN + sin);
            float yP1PSin = (yP1 + sin);
            float xFPCos = (xF + cos);
            if (cos > 0)
            {
                AddShadowTriangle(new Vector2(xF, yN), new Vector2(xFPCos, yPSin), new Vector2(xF, yP1), chunkX, chunkY);
                AddShadowTriangle(new Vector2(xF, yP1), new Vector2(xFPCos, yPSin), new Vector2(xFPCos, yP1PSin), chunkX, chunkY);
            }
            else
            {
                AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yN), new Vector2(xF, yP1), chunkX, chunkY);
                AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yP1), new Vector2(xFPCos, yP1PSin), chunkX, chunkY);
            }
        }

        internal void AddYShadow(int xN, int xP1, int yF, float cos, float sin, int chunkX, int chunkY)
        {
            float xPCos = (xN + cos);
            float xP1PCos = (xP1 + cos);
            float yFPSin = (yF + sin);
            if (sin > 0)
            {
                AddShadowTriangle(new Vector2(xN, yF), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin), chunkX, chunkY);
                AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xP1PCos, yFPSin), new Vector2(xPCos, yFPSin), chunkX, chunkY);
            }
            else
            {
                AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xN, yF), new Vector2(xPCos, yFPSin), chunkX, chunkY);
                AddShadowTriangle(new Vector2(xP1PCos, yFPSin), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin), chunkX, chunkY);
            }
        }

        internal void AddXShadow2(int yN, int yP1, int xF, float cos, float sin, int chunkX, int chunkY)
        {
            float yPSin = (yN + sin);
            float yP1PSin = (yP1 + sin);
            float xFPCos = (xF + cos);
            if (sin > 0)
            {
                if (cos > 0)
                {
                    AddShadowTriangle(new Vector2(xF, yN), new Vector2(xFPCos, yPSin), new Vector2(xF, yP1), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xF, yP1), new Vector2(xFPCos, yPSin), new Vector2(xFPCos, yP1), chunkX, chunkY);
                }
                else
                {
                    AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yN), new Vector2(xF, yP1), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yP1), new Vector2(xFPCos, yP1), chunkX, chunkY);
                }
            }
            else
            {
                if (cos > 0)
                {
                    AddShadowTriangle(new Vector2(xF, yN), new Vector2(xFPCos, yN), new Vector2(xF, yP1), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xF, yP1), new Vector2(xFPCos, yN), new Vector2(xFPCos, yP1PSin), chunkX, chunkY);
                }
                else
                {
                    AddShadowTriangle(new Vector2(xFPCos, yN), new Vector2(xF, yN), new Vector2(xF, yP1), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xFPCos, yN), new Vector2(xF, yP1), new Vector2(xFPCos, yP1PSin), chunkX, chunkY);
                }
            }
        }

        internal void AddYShadow2(int xN, int xP1, int yF, float cos, float sin, int chunkX, int chunkY)
        {
            float xPCos = (xN + cos);
            float xP1PCos = (xP1 + cos);
            float yFPSin = (yF + sin);
            if (cos > 0)
            {
                if (sin > 0)
                {
                    AddShadowTriangle(new Vector2(xN, yF), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xP1, yFPSin), new Vector2(xPCos, yFPSin), chunkX, chunkY);
                }
                else
                {
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xN, yF), new Vector2(xPCos, yFPSin), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xP1, yFPSin), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin), chunkX, chunkY);
                }
            }
            else
            {
                if (sin > 0)
                {
                    AddShadowTriangle(new Vector2(xN, yF), new Vector2(xP1, yF), new Vector2(xN, yFPSin), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xP1PCos, yFPSin), new Vector2(xN, yFPSin), chunkX, chunkY);
                }
                else
                {
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xN, yF), new Vector2(xN, yFPSin), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xP1PCos, yFPSin), new Vector2(xP1, yF), new Vector2(xN, yFPSin), chunkX, chunkY);
                }
            }
        }

        internal void AddCornerShadow(int xN, int xP1, int xF, int yN, int yP1, int yF, float cos, float sin, int chunkX, int chunkY)
        {
            float xPCos = (xN + cos);
            float xP1PCos = (xP1 + cos);
            float xFPCos = (xF + cos);
            float yPSin = (yN + sin);
            float yP1PSin = (yP1 + sin);
            float yFPSin = (yF + sin);
            if (sin > 0)
            {
                if (cos > 0)
                {
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yFPSin), new Vector2(xF, yFPSin), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yF), new Vector2(xFPCos, yFPSin), chunkX, chunkY);
                }
                else
                {
                    AddShadowTriangle(new Vector2(xF, yFPSin), new Vector2(xFPCos, yFPSin), new Vector2(xF, yF), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xFPCos, yFPSin), new Vector2(xFPCos, yF), new Vector2(xF, yF), chunkX, chunkY);
                }
            }
            else
            {
                if (cos > 0)
                {
                    AddShadowTriangle(new Vector2(xF, yFPSin), new Vector2(xFPCos, yFPSin), new Vector2(xF, yF), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xFPCos, yFPSin), new Vector2(xFPCos, yF), new Vector2(xF, yF), chunkX, chunkY);
                }
                else
                {
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yFPSin), new Vector2(xF, yFPSin), chunkX, chunkY);
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yF), new Vector2(xFPCos, yFPSin), chunkX, chunkY);
                }
            }
        }

        internal void SetShadowAngle(float angle)
        {
            if (_shadowAngle == angle)
                return;
            _shadowAngle = angle;
            _shadowCos = Math.Cos(_shadowAngle);
            _shadowSin = Math.Sin(_shadowAngle);
            ShadowCosWall = (float)(_shadowCos * _shadowDistWall);
            ShadowSinWall = (float)(_shadowSin * _shadowDistWall);
            ShadowCosObstacle = (float)(_shadowCos * _shadowDistObstacle);
            ShadowSinObstacle = (float)(_shadowSin * _shadowDistObstacle);
            ShadowVerticeCount = 0;
            for (int x = 0; x < ChunksWidth; x++)
                for (int y = 0; y < ChunksHeight; y++)
                    if (Chunks[x, y].HasBaked)
                    {
                        Chunks[x, y].ShadowVerticeCount = 0;
                        for (int j = 0; j < ChunkSize; j++)
                            for (int k = 0; k < ChunkSize; k++)
                            {
                                int tX = ((x * ChunkSize) + j);
                                int tY = ((y * ChunkSize) + k);
                                UpdateShadow(tX, tY);
                                Chunks[x, y].UpdateShadow(j, k);
                            }
                        Array.Copy(Chunks[x, y].ShadowVertices, 0, ShadowVertices, ShadowVerticeCount, Chunks[x, y].ShadowVerticeCount);
                        ShadowVerticeCount += Chunks[x, y].ShadowVerticeCount;
                    }
            ShadowsNeedRebake = true;
        }

        public static Map Load(string filePath)
        {
            Map map = null;
            using (BinaryReader br = new BinaryReader(new FileStream(filePath, FileMode.Open, FileAccess.Read), Encoding.ASCII))
                while (br.BaseStream.Position < br.BaseStream.Length)
                {
                    var header = br.ReadBmaxString();
                    if (header == "Unreal Software's CS2D Map File (max)")
                    {
                        bool scrollBackgroundLikeTiles = ((br.ReadByte() == 1) ? true : false);
                        bool useModifiers = ((br.ReadByte() == 1) ? true : false);
                        byte saveTileHeights = br.ReadByte();
                        for (var i = 0; i < 7; i++)
                            br.ReadByte();
                        for (var i = 0; i < 10; i++)
                            br.ReadInt32();
                        for (var i = 0; i < 10; i++)
                            br.ReadBmaxString();
                        br.ReadBmaxString();
                        string tilesetPath = br.ReadBmaxString();
                        int tileCount = (br.ReadByte() + 1);
                        int tilesWidth = (br.ReadInt32() + 1);
                        int tilesHeight = (br.ReadInt32() + 1);
                        using (FileStream stream = File.Open(string.Format(".\\{0}", tilesetPath), FileMode.Open, FileAccess.Read))
                            map = new Map(32, Texture2D.FromStream(Program.Game.GraphicsDevice, stream), tilesWidth, tilesHeight);
                        //map.Shadows = new Shadow[map.Tiles.GetLength(0), map.Tiles.GetLength(1)];
                        string backgroundPath = br.ReadBmaxString();
                        Vector2 backgroundScrollSpeed = new Vector2(br.ReadInt32(), br.ReadInt32());
                        for (var i = 0; i < 3; i++)
                            br.ReadByte();
                        br.ReadBmaxString();
                        for (var i = 0; i < tileCount; i++)
                            map.TileMode[i] = (TileModes)br.ReadByte();
                        if (saveTileHeights == 1)
                            for (var i = 0; i < tileCount; i++)
                                br.ReadInt32();
                        else if (saveTileHeights == 2)
                            for (var i = 0; i < tileCount; i++)
                            {
                                br.ReadInt16();
                                br.ReadByte();
                            }
                        for (var x = 0; x < tilesWidth; x++)
                            for (var y = 0; y < tilesHeight; y++)
                            {
                                int chunkX = (x >> ChunkSizeBits);
                                int chunkY = (y >> ChunkSizeBits);
                                int chunkTileX = (x & ChunkSizeModulo);
                                int chunkTileY = (y & ChunkSizeModulo);
                                map.Chunks[chunkX, chunkY].Tiles[chunkTileX, chunkTileY] = new Tile() { Id = br.ReadByte() };
                                if (useModifiers)
                                {
                                    //var modifier = br.ReadByte();
                                }
                            }
                        for (var x = 0; x < tilesWidth; x++)
                            for (var y = 0; y < tilesHeight; y++)
                                map.UpdateShadow(x, y);
                        //var entities = br.ReadInt32();
                        //for (var i = 0; i < entities; i++)
                        //{
                        //    var entity = new Entity(br.ReadBmaxString(), br.ReadByte(), br.ReadInt32(), br.ReadInt32(), br.ReadBmaxString());
                        //    for (var j = 0; j < 10; j++) entity.Triggers.Add(new Entity.Trigger(br.ReadInt32(), br.ReadBmaxString()));
                        //    map.Entities.Add(entity);
                        //}
                    }
                }
            return map;
        }
    }

    public static class BinaryReaderExtensions
    {
        private static readonly byte carriageReturn = (byte)'\r';
        private static readonly byte newLine = (byte)'\n';

        public static string ReadBmaxString(this BinaryReader br)
        {
            StringBuilder stringBuilder = new StringBuilder();
            long length = br.BaseStream.Length;
            while (br.BaseStream.Position < length)
            {
                byte b = br.ReadByte();
                if (b == carriageReturn)
                {
                    if (br.BaseStream.Position < length)
                        br.ReadByte();
                    break;
                }
                char c = Convert.ToChar(b);
                stringBuilder.Append(c);
            }
            return stringBuilder.ToString();
        }

        public static void WriteBmaxString(this BinaryWriter bw, string r)
        {
            byte[] charBytes = Encoding.ASCII.GetBytes(r);
            bw.Write(charBytes);
            bw.Write(carriageReturn);
            bw.Write(newLine);
        }
    }
}