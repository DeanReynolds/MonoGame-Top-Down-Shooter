using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Top_Down_Shooter
{
    public struct Line
    {
        public float BaseStartX { get { return _baseStartX; } }
        public float BaseStartY { get { return _baseStartY; } }
        public float BaseEndX { get { return _baseEndX; } }
        public float BaseEndY { get { return _baseEndY; } }

        public float StartX
        {
            get { return _startX; }
            set
            {
                _start.X = _startX = value;
                _angle = (float)System.Math.Atan2((_endY - _startY), (_endX - _startX));
                _distance = Vector2.Distance(_start, _end);
            }
        }
        public float StartY
        {
            get { return _startY; }
            set
            {
                _start.Y = _startY = value;
                _angle = (float)System.Math.Atan2((_endY - _startY), (_endX - _startX));
                _distance = Vector2.Distance(_start, _end);
            }
        }
        public float EndX
        {
            get { return _endX; }
            set
            {
                _end.X = _endX = value;
                _angle = (float)System.Math.Atan2((_endY - _startY), (_endX - _startX));
                _distance = Vector2.Distance(_start, _end);
            }
        }
        public float EndY
        {
            get { return _endY; }
            set
            {
                _end.Y = _endY = value;
                _angle = (float)System.Math.Atan2((_endY - _startY), (_endX - _startX));
                _distance = Vector2.Distance(_start, _end);
            }
        }
        public Vector2 Start
        {
            get { return _start; }
            set
            {
                _start.X = _startX = value.X;
                _start.Y = _startY = value.Y;
                _angle = (float)System.Math.Atan2((_endY - _startY), (_endX - _startX));
                _distance = Vector2.Distance(_start, _end);
            }
        }
        public Vector2 End
        {
            get { return _end; }
            set
            {
                _end.X = _endX = value.X;
                _end.Y = _endY = value.Y;
                _angle = (float)System.Math.Atan2((_endY - _startY), (_endX - _startX));
                _distance = Vector2.Distance(_start, _end);
            }
        }

        private float _baseStartX;
        private float _baseStartY;
        private float _baseEndX;
        private float _baseEndY;
        private float _startX;
        private float _startY;
        private float _endX;
        private float _endY;
        private float _angle;
        private float _distance;
        private Vector2 _start;
        private Vector2 _end;

        public Line(Vector2 start, Vector2 end) : this(start.X, start.Y, end.X, end.Y) { }
        public Line(Vector2 start, float endX, float endY) : this(start.X, start.Y, endX, endY) { }
        public Line(float startX, float startY, Vector2 end) : this(startX, startY, end.X, end.Y) { }
        public Line(float startX, float startY, float endX, float endY)
        {
            _start.X = _startX = _baseStartX = startX;
            _start.Y = _startY = _baseStartY = startY;
            _end.X = _endX = _baseEndX = endX;
            _end.Y = _endY = _baseEndY = endY;
            _angle = (float)System.Math.Atan2((_endY - _startY), (_endX - _startX));
            _distance = Vector2.Distance(_start, _end);
        }

        public void Draw(SpriteBatch spriteBatch, float thickness, Color color, float layer)
        {
            spriteBatch.Draw(Game1.Pixel, Start, null, color, _angle, Vector2.Zero, new Vector2(_distance, thickness), SpriteEffects.None, layer);
        }

        public bool Intersects(Line line)
        {
            float aX = (_endX - _startX);
            float aY = (_endY - _startY);
            float bX = (line._endX - line._startX);
            float bY = (line._endY - line._startY);
            float cP = (aX * bY - aY * bX);
            if (cP == 0)
                return false;
            float cX = (line._startX - _startX);
            float cY = (line._startY - _startY);
            float t = ((cX * bY - cY * bX) / cP);
            if (t < 0 || t > 1)
                return false;
            float u = ((cX * aY - cY * aX) / cP);
            if (u < 0 || u > 1)
                return false;
            return true;
        }
        public bool Intersects(Line line, out Vector2 intersection)
        {
            intersection = Vector2.Zero;
            float aX = (_endX - _startX);
            float aY = (_endY - _startY);
            float bX = (line._endX - line._startX);
            float bY = (line._endY - line._startY);
            float cP = (aX * bY - aY * bX);
            if (cP == 0)
                return false;
            float cX = (line._startX - _startX);
            float cY = (line._startY - _startY);
            float t = ((cX * bY - cY * bX) / cP);
            if (t < 0 || t > 1)
                return false;
            float u = ((cX * aY - cY * aX) / cP);
            if (u < 0 || u > 1)
                return false;
            intersection.X = (_startX + t * aX);
            intersection.Y = (_startY + t * aY);
            return true;
        }
        public bool Intersects(Polygon polygon) { return polygon.Intersects(this); }
        public bool Intersects(Polygon polygon, ref Vector2 intersection) { return polygon.Intersects(this, out intersection); }

        public override int GetHashCode() { return (_startX.GetHashCode() * 17 + _startY.GetHashCode() * 17 + _endX.GetHashCode() * 17 + _endY.GetHashCode() * 17); }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return ReferenceEquals(this, null);
            if (!(obj is Line))
                return false;
            Line other = (Line)obj;
            return ((_startX == other._startX) && (_startY == other._startY) && (_endX == other._endX) && (_endY == other._endY));
        }
        public static bool operator !=(Line line, Line other) { return !(line == other); }
        public static bool operator ==(Line line, Line other) { return (ReferenceEquals(line, other) || (!ReferenceEquals(line, null) && line.Equals(other))); }
        public static Line operator +(Line line, Vector2 position) { return new Line((line._startX + position.X), (line._startY + position.Y), (line._endX + position.X), (line._endY + position.Y)); }
        public static Line operator -(Line line, Vector2 position) { return new Line((line._startX - position.X), (line._startY - position.Y), (line._endX - position.X), (line._endY - position.Y)); }
    }
}