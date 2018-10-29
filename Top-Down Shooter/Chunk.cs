using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Top_Down_Shooter
{
    public class Chunk
    {
        public Tile[,] Tiles { get; internal set; }
        
        internal RenderTarget2D Texture;
        //internal VertexPositionTexture[] TileVertices { get; private set; }
        //internal int[] TileIndices { get; private set; }
        //internal int TileVerticeCount { get; private set; }
        //internal int TileIndiceCount { get; private set; }
        internal bool HasBaked { get; private set; }

        internal readonly VertexPositionColor[] ShadowVertices;

        internal int ShadowVerticeCount;

        private Map _map;
        private int _x;
        private int _y;

        public Chunk(Map map, int x, int y)
        {
            _map = map;
            _x = x;
            _y = y;
            Tiles = new Tile[Map.ChunkSize, Map.ChunkSize];
            //TileVertices = new VertexPositionTexture[(Map.ChunkSize * Map.ChunkSize) * 4];
            //TileIndices = new int[(Map.ChunkSize * Map.ChunkSize) * 6];
            //for (int x = 0; x < Map.ChunkSize; x++)
            //    for (int y = 0; y < Map.ChunkSize; y++)
            //    {
            //        Rectangle s = map.TileSource[tileId];
            //        Vector2 textureUpperLeft = new Vector2((s.X / (float)map.TileSheet.Width), (s.Y / (float)map.TileSheet.Height));
            //        Vector2 textureUpperRight = new Vector2(((s.X + s.Width) / (float)map.TileSheet.Width), (s.Y / (float)map.TileSheet.Height));
            //        Vector2 textureLowerLeft = new Vector2((s.X / (float)map.TileSheet.Width), ((s.Y + s.Height) / (float)map.TileSheet.Height));
            //        Vector2 textureLowerRight = new Vector2(((s.X + s.Width) / (float)map.TileSheet.Width), ((s.Y + s.Height) / (float)map.TileSheet.Height));
            //        int j = (x * map.TileSize);
            //        int k = -(y * map.TileSize);
            //        int n = ((x + 1) * map.TileSize);
            //        int m = -((y + 1) * map.TileSize);
            //        int a = TileVerticeCount;
            //        TileVertices[TileVerticeCount].Position = new Vector3(j, m, 0);
            //        TileVertices[TileVerticeCount++].TextureCoordinate = textureLowerLeft;
            //        TileVertices[TileVerticeCount].Position = new Vector3(j, k, 0);
            //        TileVertices[TileVerticeCount++].TextureCoordinate = textureUpperLeft;
            //        TileVertices[TileVerticeCount].Position = new Vector3(n, m, 0);
            //        TileVertices[TileVerticeCount++].TextureCoordinate = textureLowerRight;
            //        TileVertices[TileVerticeCount].Position = new Vector3(n, k, 0);
            //        TileVertices[TileVerticeCount++].TextureCoordinate = textureUpperRight;
            //        TileIndeces[TileIndeceCount++] = a;
            //        TileIndeces[TileIndeceCount++] = (a + 1);
            //        TileIndeces[TileIndeceCount++] = (a + 2);
            //        TileIndeces[TileIndeceCount++] = (a + 2);
            //        TileIndeces[TileIndeceCount++] = (a + 1);
            //        TileIndeces[TileIndeceCount++] = (a + 3);
            //    }
            ShadowVertices = new VertexPositionColor[18 * (Map.ChunkSize * Map.ChunkSize)];
        }

        public void Bake(RenderTarget2D rt = null)
        {
            if ((rt != null) && !ReferenceEquals(rt, Texture))
            {
                Texture = rt;
                Texture.ContentLost += Texture_ContentLost;
            }
            ShadowVerticeCount = 0;
            SpriteBatch spriteBatch = Program.Game.Services.GetService<SpriteBatch>();
            Program.Game.GraphicsDevice.SetRenderTarget(Texture);
            Program.Game.GraphicsDevice.Clear(Color.TransparentBlack);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, null);
            for (int x = 0; x < Map.ChunkSize; x++)
                for (int y = 0; y < Map.ChunkSize; y++)
                    if (Tiles[x, y] != null)
                        if (Tiles[x, y].Id > 0)
                        {
                            spriteBatch.Draw(_map.TileSheet, _map.TileDestination[x, y], _map.TileSource[Tiles[x, y].Id], Color.White);
                            UpdateShadow(x, y);
                        }
            if (Game1.DebugMenu.Groups["D2D"].Items["Chunk Bound Lines"].SelectedOption?.Text == "On")
            {
                Game1.WorldBakeDrawCount += (Program.Game.GraphicsDevice.Metrics.DrawCount - Game1.WorldBakeDrawCount);
                Game1.WorldBakeTextureCount += (Program.Game.GraphicsDevice.Metrics.TextureCount - Game1.WorldBakeTextureCount);
                Game1.WorldBakeSpriteCount += (Program.Game.GraphicsDevice.Metrics.SpriteCount - Game1.WorldBakeSpriteCount);
                Game1.WorldBakePrimitiveCount += (Program.Game.GraphicsDevice.Metrics.PrimitiveCount - Game1.WorldBakePrimitiveCount);
                Game1.WorldBakeTargetCount += (Program.Game.GraphicsDevice.Metrics.TargetCount - Game1.WorldBakeTargetCount);
                Color color = (Color.Red * .5f);
                spriteBatch.Draw(Game1.Pixel, new Rectangle(0, 0, _map.ChunkTextureSize, 1), null, color, 0, Vector2.Zero, SpriteEffects.None, 0);
                spriteBatch.Draw(Game1.Pixel, new Rectangle((_map.ChunkTextureSize - 1), 1, 1, (_map.ChunkTextureSize - 1)), null, color, 0, Vector2.Zero, SpriteEffects.None, 0);
                spriteBatch.Draw(Game1.Pixel, new Rectangle(0, (_map.ChunkTextureSize - 1), (_map.ChunkTextureSize - 1), 1), null, color, 0, Vector2.Zero, SpriteEffects.None, 0);
                spriteBatch.Draw(Game1.Pixel, new Rectangle(0, 1, 1, (_map.ChunkTextureSize - 2)), null, color, 0, Vector2.Zero, SpriteEffects.None, 0);
            }
            spriteBatch.End();
            //TileVerticeCount = 0;
            //TileIndiceCount = 0;
            //for (int x = 0; x < Map.ChunkSize; x++)
            //    for (int y = 0; y < Map.ChunkSize; y++)
            //        if (Tiles[x, y] != null)
            //            if (Tiles[x, y].Id > 0)
            //            {
            //                Rectangle s = _map.TileSource[Tiles[x, y].Id];
            //                Vector2 textureUpperLeft = new Vector2((s.X / (float)_map.TileSheet.Width), (s.Y / (float)_map.TileSheet.Height));
            //                Vector2 textureUpperRight = new Vector2(((s.X + s.Width) / (float)_map.TileSheet.Width), (s.Y / (float)_map.TileSheet.Height));
            //                Vector2 textureLowerLeft = new Vector2((s.X / (float)_map.TileSheet.Width), ((s.Y + s.Height) / (float)_map.TileSheet.Height));
            //                Vector2 textureLowerRight = new Vector2(((s.X + s.Width) / (float)_map.TileSheet.Width), ((s.Y + s.Height) / (float)_map.TileSheet.Height));
            //                int j = ((_x * _map.ChunkTextureSize) + (x * _map.TileSize));
            //                int k = -((_y * _map.ChunkTextureSize) + (y * _map.TileSize));
            //                int n = ((_x * _map.ChunkTextureSize) + ((x + 1) * _map.TileSize));
            //                int m = -((_y * _map.ChunkTextureSize) + ((y + 1) * _map.TileSize));
            //                int a = TileVerticeCount;
            //                TileVertices[TileVerticeCount].Position = new Vector3(j, m, 0);
            //                TileVertices[TileVerticeCount++].TextureCoordinate = textureLowerLeft;
            //                TileVertices[TileVerticeCount].Position = new Vector3(j, k, 0);
            //                TileVertices[TileVerticeCount++].TextureCoordinate = textureUpperLeft;
            //                TileVertices[TileVerticeCount].Position = new Vector3(n, m, 0);
            //                TileVertices[TileVerticeCount++].TextureCoordinate = textureLowerRight;
            //                TileVertices[TileVerticeCount].Position = new Vector3(n, k, 0);
            //                TileVertices[TileVerticeCount++].TextureCoordinate = textureUpperRight;
            //                TileIndices[TileIndiceCount++] = a;
            //                TileIndices[TileIndiceCount++] = (a + 1);
            //                TileIndices[TileIndiceCount++] = (a + 2);
            //                TileIndices[TileIndiceCount++] = (a + 2);
            //                TileIndices[TileIndiceCount++] = (a + 1);
            //                TileIndices[TileIndiceCount++] = (a + 3);
            //            }
            HasBaked = true;
        }

        private void Texture_ContentLost(object sender, EventArgs e)
        {
            Bake();
        }

        internal void ReleaseTexture()
        {
            Texture.ContentLost -= Texture_ContentLost;
            Texture = null;
            HasBaked = false;
        }

        internal void UpdateShadow(int x, int y)
        {
            if (_map.IsWall(_map.TileMode[Tiles[x, y].Id]))
            {
                int xN = (x * _map.TileSize);
                int xP1 = ((x + 1) * _map.TileSize);
                int xF = ((_map.ShadowCosWall > 0) ? xP1 : xN);
                int yN = (y * _map.TileSize);
                int yP1 = ((y + 1) * _map.TileSize);
                int yF = ((_map.ShadowSinWall > 0) ? yP1 : yN);
                if (!_map.IsWall(Tiles[x, y].TileXMode) || !_map.IsWall(Tiles[x, y].TileYMode))
                    if (_map.IsWall(Tiles[x, y].TileXMode))
                    {
                        if (_map.IsObstacle(Tiles[x, y].TileYMode))
                        {
                            if (!_map.IsWall(Tiles[x, y].TileXYMode))
                                AddYShadow(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                            else
                                AddYShadow2(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        }
                        else
                        {
                            if (_map.IsWall(Tiles[x, y].TileXYMode))
                                AddYShadow2(xN, xP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                            else if (_map.IsObstacle(Tiles[x, y].TileXYMode))
                            {
                                AddYShadow2(xN, xP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                                AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                            }
                            else
                                AddYShadow(xN, xP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                        }
                    }
                    else if (_map.IsObstacle(Tiles[x, y].TileXMode))
                    {
                        if (!_map.IsWall(Tiles[x, y].TileYMode))
                        {
                            if (!_map.IsWall(Tiles[x, y].TileXYMode))
                            {
                                if (_map.IsObstacle(Tiles[x, y].TileXYMode))
                                {
                                    if (_map.IsObstacle(Tiles[x, y].TileYMode))
                                    {
                                        AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                        AddYShadow2(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                        AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                    }
                                    else
                                    {
                                        AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                        AddYShadow2(xN, xP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                                        AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                    }
                                }
                                else
                                {
                                    if (_map.IsObstacle(Tiles[x, y].TileYMode))
                                    {
                                        AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                        AddYShadow2(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                        AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                                    }
                                    else
                                    {
                                        AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                                        AddYShadow2(xN, xP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                                        AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                                    }
                                }
                            }
                            else
                                AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        }
                        else if (!_map.IsWall(Tiles[x, y].TileXYMode))
                        {
                            AddXShadow(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                            if (!_map.IsObstacle(Tiles[x, y].TileXYMode))
                                AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                        }
                        else
                            AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                    }
                    else if (_map.IsWall(Tiles[x, y].TileYMode))
                    {
                        if (_map.IsWall(Tiles[x, y].TileXYMode))
                            AddXShadow2(yN, yP1, xF, _map.ShadowCosWall, _map.ShadowSinWall);
                        else if (_map.IsObstacle(Tiles[x, y].TileXYMode))
                        {
                            AddXShadow2(yN, yP1, xF, _map.ShadowCosWall, _map.ShadowSinWall);
                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        }
                        else
                            AddXShadow(yN, yP1, xF, _map.ShadowCosWall, _map.ShadowSinWall);
                    }
                    else if (_map.IsObstacle(Tiles[x, y].TileYMode))
                    {
                        AddXShadow2(yN, yP1, xF, _map.ShadowCosWall, _map.ShadowSinWall);
                        AddYShadow2(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        if (_map.IsObstacle(Tiles[x, y].TileXYMode))
                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        else if (!_map.IsWall(Tiles[x, y].TileXYMode))
                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                    }
                    else
                    {
                        AddXShadow2(yN, yP1, xF, _map.ShadowCosWall, _map.ShadowSinWall);
                        AddYShadow2(xN, xP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                        if (_map.IsObstacle(Tiles[x, y].TileXYMode))
                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        else if (!_map.IsWall(Tiles[x, y].TileXYMode))
                            AddCornerShadow(xN, xP1, xF, yN, yP1, yF, _map.ShadowCosWall, _map.ShadowSinWall);
                    }
            }
            else if (_map.IsObstacle(_map.TileMode[Tiles[x, y].Id]))
            {
                int xN = (x * _map.TileSize);
                int xP1 = ((x + 1) * _map.TileSize);
                int xF = ((_map.ShadowCosObstacle > 0) ? xP1 : xN);
                int yN = (y * _map.TileSize);
                int yP1 = ((y + 1) * _map.TileSize);
                int yF = ((_map.ShadowSinObstacle > 0) ? yP1 : yN);
                if (!_map.IsWallOrObstacle(Tiles[x, y].TileXMode) || !_map.IsWallOrObstacle(Tiles[x, y].TileYMode))
                    if (_map.IsWallOrObstacle(Tiles[x, y].TileXMode))
                    {
                        if (_map.IsWallOrObstacle(Tiles[x, y].TileXYMode))
                            AddYShadow2(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        else
                            AddYShadow(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                    }
                    else if (_map.IsWallOrObstacle(Tiles[x, y].TileYMode))
                    {
                        if (_map.IsWallOrObstacle(Tiles[x, y].TileXYMode))
                            AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        else
                            AddXShadow(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                    }
                    else if (_map.IsWallOrObstacle(Tiles[x, y].TileXYMode))
                    {
                        AddXShadow2(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        AddYShadow2(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                    }
                    else
                    {
                        AddXShadow(yN, yP1, xF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                        AddYShadow(xN, xP1, yF, _map.ShadowCosObstacle, _map.ShadowSinObstacle);
                    }
            }
        }

        internal void AddShadowTriangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            int j = (_x * _map.ChunkTextureSize);
            int k = (_y * _map.ChunkTextureSize);
            Color color = Color.Black;
            ShadowVertices[ShadowVerticeCount].Position = new Vector3((j + p1.X), (k + p1.Y), -0.1f);
            ShadowVertices[ShadowVerticeCount++].Color = color;
            ShadowVertices[ShadowVerticeCount].Position = new Vector3((j + p2.X), (k + p2.Y), -0.1f);
            ShadowVertices[ShadowVerticeCount++].Color = color;
            ShadowVertices[ShadowVerticeCount].Position = new Vector3((j + p3.X), (k + p3.Y), -0.1f);
            ShadowVertices[ShadowVerticeCount++].Color = color;
        }

        internal void AddXShadow(int yN, int yP1, int xF, float cos, float sin)
        {
            float yPSin = (yN + sin);
            float yP1PSin = (yP1 + sin);
            float xFPCos = (xF + cos);
            if (cos > 0)
            {
                AddShadowTriangle(new Vector2(xF, yN), new Vector2(xFPCos, yPSin), new Vector2(xF, yP1));
                AddShadowTriangle(new Vector2(xF, yP1), new Vector2(xFPCos, yPSin), new Vector2(xFPCos, yP1PSin));
            }
            else
            {
                AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yN), new Vector2(xF, yP1));
                AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yP1), new Vector2(xFPCos, yP1PSin));
            }
        }

        internal void AddYShadow(int xN, int xP1, int yF, float cos, float sin)
        {
            float xPCos = (xN + cos);
            float xP1PCos = (xP1 + cos);
            float yFPSin = (yF + sin);
            if (sin > 0)
            {
                AddShadowTriangle(new Vector2(xN, yF), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin));
                AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xP1PCos, yFPSin), new Vector2(xPCos, yFPSin));
            }
            else
            {
                AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xN, yF), new Vector2(xPCos, yFPSin));
                AddShadowTriangle(new Vector2(xP1PCos, yFPSin), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin));
            }
        }

        internal void AddXShadow2(int yN, int yP1, int xF, float cos, float sin)
        {
            float yPSin = (yN + sin);
            float yP1PSin = (yP1 + sin);
            float xFPCos = (xF + cos);
            if (sin > 0)
            {
                if (cos > 0)
                {
                    AddShadowTriangle(new Vector2(xF, yN), new Vector2(xFPCos, yPSin), new Vector2(xF, yP1));
                    AddShadowTriangle(new Vector2(xF, yP1), new Vector2(xFPCos, yPSin), new Vector2(xFPCos, yP1));
                }
                else
                {
                    AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yN), new Vector2(xF, yP1));
                    AddShadowTriangle(new Vector2(xFPCos, yPSin), new Vector2(xF, yP1), new Vector2(xFPCos, yP1));
                }
            }
            else
            {
                if (cos > 0)
                {
                    AddShadowTriangle(new Vector2(xF, yN), new Vector2(xFPCos, yN), new Vector2(xF, yP1));
                    AddShadowTriangle(new Vector2(xF, yP1), new Vector2(xFPCos, yN), new Vector2(xFPCos, yP1PSin));
                }
                else
                {
                    AddShadowTriangle(new Vector2(xFPCos, yN), new Vector2(xF, yN), new Vector2(xF, yP1));
                    AddShadowTriangle(new Vector2(xFPCos, yN), new Vector2(xF, yP1), new Vector2(xFPCos, yP1PSin));
                }
            }
        }

        internal void AddYShadow2(int xN, int xP1, int yF, float cos, float sin)
        {
            float xPCos = (xN + cos);
            float xP1PCos = (xP1 + cos);
            float yFPSin = (yF + sin);
            if (cos > 0)
            {
                if (sin > 0)
                {
                    AddShadowTriangle(new Vector2(xN, yF), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin));
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xP1, yFPSin), new Vector2(xPCos, yFPSin));
                }
                else
                {
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xN, yF), new Vector2(xPCos, yFPSin));
                    AddShadowTriangle(new Vector2(xP1, yFPSin), new Vector2(xP1, yF), new Vector2(xPCos, yFPSin));
                }
            }
            else
            {
                if (sin > 0)
                {
                    AddShadowTriangle(new Vector2(xN, yF), new Vector2(xP1, yF), new Vector2(xN, yFPSin));
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xP1PCos, yFPSin), new Vector2(xN, yFPSin));
                }
                else
                {
                    AddShadowTriangle(new Vector2(xP1, yF), new Vector2(xN, yF), new Vector2(xN, yFPSin));
                    AddShadowTriangle(new Vector2(xP1PCos, yFPSin), new Vector2(xP1, yF), new Vector2(xN, yFPSin));
                }
            }
        }

        internal void AddCornerShadow(int xN, int xP1, int xF, int yN, int yP1, int yF, float cos, float sin)
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
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yFPSin), new Vector2(xF, yFPSin));
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yF), new Vector2(xFPCos, yFPSin));
                }
                else
                {
                    AddShadowTriangle(new Vector2(xF, yFPSin), new Vector2(xFPCos, yFPSin), new Vector2(xF, yF));
                    AddShadowTriangle(new Vector2(xFPCos, yFPSin), new Vector2(xFPCos, yF), new Vector2(xF, yF));
                }
            }
            else
            {
                if (cos > 0)
                {
                    AddShadowTriangle(new Vector2(xF, yFPSin), new Vector2(xFPCos, yFPSin), new Vector2(xF, yF));
                    AddShadowTriangle(new Vector2(xFPCos, yFPSin), new Vector2(xFPCos, yF), new Vector2(xF, yF));
                }
                else
                {
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yFPSin), new Vector2(xF, yFPSin));
                    AddShadowTriangle(new Vector2(xF, yF), new Vector2(xFPCos, yF), new Vector2(xFPCos, yFPSin));
                }
            }
        }
    }
}