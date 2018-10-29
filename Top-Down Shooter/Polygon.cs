using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Top_Down_Shooter
{
    public class Polygon
    {
        public float X
        {
            get { return _x; }
            set
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    Lines[i].StartX -= _x;
                    Lines[i].EndX -= _x;
                    Lines[i].StartX += value;
                    Lines[i].EndX += value;
                }
                _position.X = _x = value;
            }
        }
        public float Y
        {
            get { return _y; }
            set
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    Lines[i].StartY -= _y;
                    Lines[i].EndY -= _y;
                    Lines[i].StartY += value;
                    Lines[i].EndY += value;
                }
                _position.Y = _y = value;
            }
        }
        public Vector2 Position
        {
            get { return _position; }
            set
            {
                for (int i = 0; i < Lines.Length; i++)
                {
                    Lines[i].StartX -= _x;
                    Lines[i].EndX -= _x;
                    Lines[i].StartX += value.X;
                    Lines[i].EndX += value.X;
                    Lines[i].StartY -= _y;
                    Lines[i].EndY -= _y;
                    Lines[i].StartY += value.Y;
                    Lines[i].EndY += value.Y;
                }
                _position.X = _x = value.X;
                _position.Y = _y = value.Y;
            }
        }
        public float Angle
        {
            get { return _angle; }
            set
            {
                _rotM11 = (float)Math.Cos(-(_angle = value));
                _rotM12 = (float)Math.Sin(-_angle);
                for (int i = 0; i < Lines.Length; i++)
                {
                    Lines[i].StartX = (((Lines[i].BaseStartX * _rotM11) + (Lines[i].BaseStartY * -_rotM12)) + _x);
                    Lines[i].StartY = (((Lines[i].BaseStartX * _rotM12) + (Lines[i].BaseStartY * _rotM11)) + _y);
                    Lines[i].EndX = (((Lines[i].BaseEndX * _rotM11) + (Lines[i].BaseEndY * -_rotM12)) + _x);
                    Lines[i].EndY = (((Lines[i].BaseEndX * _rotM12) + (Lines[i].BaseEndY * _rotM11)) + _y);
                }
            }
        }

        public Line[] Lines { get; private set; }
        public float BaseWidth { get; private set; }
        public float BaseHeight { get; private set; }

        public float MinX
        {
            get
            {
                float minX = float.MaxValue;
                foreach (Line line in Lines)
                    minX = MathHelper.Min(minX, MathHelper.Min(line.Start.X, line.End.X));
                return minX;
            }
        }
        public float MinY
        {
            get
            {
                float minY = float.MaxValue;
                foreach (Line line in Lines)
                    minY = MathHelper.Min(minY, MathHelper.Min(line.Start.Y, line.End.Y));
                return minY;
            }
        }
        public float MaxX
        {
            get
            {
                float maxX = float.MinValue;
                foreach (Line line in Lines)
                    maxX = MathHelper.Max(maxX, MathHelper.Max(line.Start.X, line.End.X));
                return maxX;
            }
        }
        public float MaxY
        {
            get
            {
                float maxY = float.MinValue;
                foreach (Line line in Lines)
                    maxY = MathHelper.Max(maxY, MathHelper.Max(line.Start.Y, line.End.Y));
                return maxY;
            }
        }
        public float Width { get { return (MaxX - MinX); } }
        public float Height { get { return (MaxY - MinY); } }

        private float _x;
        private float _y;
        private Vector2 _position;
        private float _angle;
        private float _rotM11;
        private float _rotM12;
        private VertexPositionColor[] _vertices;
        private bool _triangulated;
        private VertexPositionColor[] _triangulatedVertices;
        private int[] _indeces;
        private Vector3 _centerPoint;

        public Polygon(Line[] lines, float angle = 0)
        {
            Lines = lines;
            BaseWidth = Width;
            BaseHeight = Height;
            Angle = angle;
            _vertices = new VertexPositionColor[lines.Length];
            for (int i = 0; i < lines.Length; i++)
                _vertices[i] = new VertexPositionColor(new Vector3(lines[i].Start, 0), Color.White);
            _triangulated = false;
            _triangulatedVertices = new VertexPositionColor[_vertices.Length * 3];
            _indeces = new int[_vertices.Length];
        }
        public Polygon(List<Line> lines, float angle = 0) : this(lines.ToArray(), angle) { }

        public void Draw(SpriteBatch spriteBatch, float thickness, Color color, float layer)
        {
            foreach (Line line in Lines)
                line.Draw(spriteBatch, thickness, color, layer);
        }

        public void Fill(GraphicsDevice graphicsDevice, BasicEffect effect)
        {
            if (!_triangulated)
                Triangulate();
            effect.CurrentTechnique.Passes[0].Apply();
            graphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _triangulatedVertices, 0, _vertices.Length);
        }

        public VertexPositionColor[] Triangulate()
        {
            float xCount = 0, yCount = 0;
            foreach (VertexPositionColor vertice in _vertices)
            {
                xCount += vertice.Position.X;
                yCount += vertice.Position.Y;
            }
            _centerPoint = new Vector3(xCount / _vertices.Length, yCount / _vertices.Length, 0);
            for (int i = 1; i < _triangulatedVertices.Length; i = i + 3)
                _indeces[i / 3] = i - 1;
            for (int i = 0; i < _indeces.Length; i++)
            {
                int index = _indeces[i];
                _triangulatedVertices[index] = _vertices[index / 3];
                if (index / 3 != _vertices.Length - 1)
                    _triangulatedVertices[index + 1] = _vertices[(index / 3) + 1];
                else
                    _triangulatedVertices[index + 1] = _vertices[0];
                _triangulatedVertices[index + 2].Position = _centerPoint;
            }
            _triangulated = true;
            return _triangulatedVertices;
        }

        public bool Intersects(Line line)
        {
            foreach (Line t in Lines)
                if (t.Intersects(line))
                    return true;
            return false;
        }
        public bool Intersects(Line line, out Vector2 intersection)
        {
            intersection = Vector2.Zero;
            Vector2 originalEnd = line.End;
            bool intersects = false;
            foreach (Line t in Lines)
                if (t.Intersects(line, out intersection))
                {
                    line.End = intersection;
                    intersects = true;
                }
            line.End = originalEnd;
            return intersects;
        }
        public bool Intersects(Polygon polygon)
        {
            foreach (Line a in Lines)
                foreach (Line t in polygon.Lines)
                    if (a.Intersects(t))
                        return true;
            return false;
        }
        public bool Intersects(Polygon polygon, ref Vector2 intersection)
        {
            foreach (Line a in Lines)
                foreach (Line t in polygon.Lines)
                    if (a.Intersects(t, out intersection))
                        return true;
            return false;
        }

        //public static Polygon CreateSquare(float radius) { return CreateRectangle(new Vector2(radius), Vector2.Zero); }
        //public static Polygon CreateSquare(float radius, Vector2 origin) { return CreateRectangle(new Vector2(radius), origin); }
        //public static Polygon CreateSquareWithCross(float radius) { return CreateRectangleWithCross(new Vector2(radius), Vector2.Zero); }
        //public static Polygon CreateSquareWithCross(float radius, Vector2 origin) { return CreateRectangleWithCross(new Vector2(radius), origin); }
        //public static Polygon CreateRectangle(Vector2 size) { return CreateRectangle(size, Vector2.Zero); }
        //public static Polygon CreateRectangle(Vector2 size, Vector2 origin)
        //{
        //    size.X += 1;
        //    size.Y += 1;
        //    float x = (size.X / 2), y = (size.Y / 2);
        //    return new Polygon(new[]
        //    {
        //        new Line((new Vector2(-x, -y) - origin), (new Vector2((x - PxOffset), -y) - origin)),
        //        new Line((new Vector2(x, -y) - origin), (new Vector2(x, (y - PxOffset)) - origin)),
        //        new Line((new Vector2(x, y) - origin), (new Vector2((-x + PxOffset), y) - origin)),
        //        new Line((new Vector2(-x, y) - origin), (new Vector2(-x, (-y + PxOffset)) - origin))
        //    });
        //}
        public static Polygon CreateCircle(float radius) { return CreateEllipse(new Vector2(radius), Vector2.Zero, 8); }
        public static Polygon CreateCircle(float radius, byte sides) { return CreateEllipse(new Vector2(radius), Vector2.Zero, sides); }
        public static Polygon CreateCircle(float radius, Vector2 origin) { return CreateEllipse(new Vector2(radius), origin, 8); }
        public static Polygon CreateCircle(float radius, Vector2 origin, byte sides) { return CreateEllipse(new Vector2(radius), origin, sides); }
        public static Polygon CreateCircleWithCross(float radius) { return CreateEllipseWithCross(new Vector2(radius), Vector2.Zero, 8); }
        public static Polygon CreateCircleWithCross(float radius, byte sides) { return CreateEllipseWithCross(new Vector2(radius), Vector2.Zero, sides); }
        public static Polygon CreateCircleWithCross(float radius, Vector2 origin) { return CreateEllipseWithCross(new Vector2(radius), origin, 8); }
        public static Polygon CreateCircleWithCross(float radius, Vector2 origin, byte sides) { return CreateEllipseWithCross(new Vector2(radius), origin, sides); }
        public static Polygon CreateEllipse(Vector2 radius) { return CreateEllipse(radius, Vector2.Zero, 8); }
        public static Polygon CreateEllipse(Vector2 radius, byte sides) { return CreateEllipse(radius, Vector2.Zero, sides); }
        public static Polygon CreateEllipse(Vector2 radius, Vector2 origin) { return CreateEllipse(radius, origin, 8); }
        public static Polygon CreateEllipse(Vector2 radius, Vector2 origin, byte sides)
        {
            float sideLengthX = (radius.X / sides * MathHelper.Pi);
            float sideLengthY = (radius.Y / sides * MathHelper.Pi);
            Vector2 start = new Vector2(origin.X, (-(radius.Y / 2) + origin.Y));
            Line[] lines = new Line[sides];
            for (var side = 0; side < sides; side++)
            {
                var angle = MathHelper.ToRadians(((side + .5f) / sides) * 360);
                var end = new Vector2((start.X + ((float)(Math.Cos(angle) * sideLengthX))), (start.Y + ((float)(Math.Sin(angle) * sideLengthY))));
                lines[side] = new Line(start, end);
                start = end;
            }
            return new Polygon(lines);
        }
        public static Polygon CreateEllipseWithCross(Vector2 radius) { return CreateEllipseWithCross(radius, Vector2.Zero, 8); }
        public static Polygon CreateEllipseWithCross(Vector2 radius, byte sides) { return CreateEllipseWithCross(radius, Vector2.Zero, sides); }
        public static Polygon CreateEllipseWithCross(Vector2 radius, Vector2 origin) { return CreateEllipseWithCross(radius, origin, 8); }
        public static Polygon CreateEllipseWithCross(Vector2 radius, Vector2 origin, byte sides)
        {
            var lines = new List<Line>(2 + sides)
            {
                new Line((new Vector2(-(radius.X/2.5f), -(radius.Y/2.5f)) - origin), (new Vector2((radius.X/2.5f), (radius.Y/2.5f)) - origin)),
                new Line((new Vector2((radius.X/2.5f), -(radius.Y/2.5f)) - origin), (new Vector2(-(radius.X/2.5f), (radius.Y/2.5f)) - origin))
            };
            float sideLengthX = (radius.X / sides * MathHelper.Pi), sideLengthY = (radius.Y / sides * MathHelper.Pi);
            var start = new Vector2(origin.X, (-(radius.Y / 2) + origin.Y));
            for (var side = 0; side < sides; side++)
            {
                var angle = MathHelper.ToRadians(((side + .5f) / sides) * 360);
                var end = new Vector2((start.X + ((float)(Math.Cos(angle) * sideLengthX))), (start.Y + ((float)(Math.Sin(angle) * sideLengthY))));
                lines.Add(new Line(start, end));
                start = end;
            }
            return new Polygon(lines.ToArray());
        }

        public static Polygon CreateCross(float radius) { return CreateCross(radius, Vector2.Zero); }
        public static Polygon CreateCross(Vector2 radius) { return CreateCross(radius, Vector2.Zero); }
        public static Polygon CreateCross(float radius, Vector2 origin)
        {
            var lines = new[]
            {
                new Line((new Vector2(-(radius/2f), -(radius/2f)) - origin), (new Vector2((radius/2f), (radius/2f)) - origin)),
                new Line((new Vector2((radius/2f), -(radius/2f)) - origin), (new Vector2(-(radius/2f), (radius/2f)) - origin))
            };
            return new Polygon(lines);
        }
        public static Polygon CreateCross(Vector2 radius, Vector2 origin)
        {
            var lines = new[]
            {
                new Line((new Vector2(-(radius.X/2f), -(radius.X/2f)) - origin), (new Vector2((radius.Y/2f), (radius.Y/2f)) - origin)),
                new Line((new Vector2((radius.X/2f), -(radius.X/2f)) - origin), (new Vector2(-(radius.Y/2f), (radius.Y/2f)) - origin))
            };
            return new Polygon(lines);
        }
    }
}