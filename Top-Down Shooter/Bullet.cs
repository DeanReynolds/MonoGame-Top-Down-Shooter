using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Top_Down_Shooter
{
    public class Bullet
    {
        public const float Layer = .000001f;

        private const float MoveSpeed = 25;

        public static Texture2D Tracer { get; internal set; }
        public static Vector2 Origin { get; internal set; }

        public float Opacity { get; private set; }

        public readonly float Angle;

        private readonly Vector2 _start;
        private readonly Vector2 _end;
        private readonly float _xAngle;
        private readonly float _yAngle;

        private Vector2 _current;
        private Vector2 _scale;
        private Color _color;
        private event UpdateEvent _updateEvents;
        private event DrawEvent _drawEvents;

        private delegate void UpdateEvent(GameTime gameTime);
        private delegate void DrawEvent(SpriteBatch spriteBatch, GameTime gameTime);

        public Bullet(Vector2 start, Vector2 end, float angle)
        {
            _current = _start = start;
            _end = end;
            //Angle = (float)Math.Atan2((end.Y - start.Y), (end.X - start.X));
            Angle = angle;
            _xAngle = (float)Math.Cos(Angle);
            _yAngle = (float)Math.Sin(Angle);
            float distance = Vector2.Distance(_current, _end);
            _scale = new Vector2((distance / Tracer.Width), .666f);
            Opacity = 1;
            _updateEvents += Update1;
            _drawEvents += Draw1;
        }

        public void Update(GameTime gameTime)
        {
            _updateEvents.Invoke(gameTime);
        }

        private void Update1(GameTime gameTime)
        {
            float distance = Vector2.Distance(_current, _end);
            if (distance <= MoveSpeed)
            {
                _scale.X = (1f / Tracer.Width);
                _color = Color.White;
                _updateEvents += Update2;
                _updateEvents -= Update1;
                _drawEvents += Draw2;
                _drawEvents -= Draw1;
            }
            else
            {
                float moveSpeed = Math.Min(distance, MoveSpeed);
                _current.X += (_xAngle * moveSpeed);
                _current.Y += (_yAngle * moveSpeed);
                distance = Vector2.Distance(_current, _end);
                _scale.X = (distance / Tracer.Width);
            }
        }

        private void Update2(GameTime gameTime)
        {
            if (Opacity > 0)
            {
                Opacity = Math.Max(0, (float)(Opacity - (gameTime.ElapsedGameTime.TotalSeconds / .5)));
                _color = (Color.White * Opacity);
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            _drawEvents.Invoke(spriteBatch, gameTime);
        }

        private void Draw1(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(Tracer, _current, null, Color.White, Angle, Origin, _scale, SpriteEffects.None, Layer);
            //spriteBatch.Draw(Game1.Pixel, _end, null, Color.Red, 0, Game1.PixelOrigin, new Vector2(3), SpriteEffects.None, Layer);
        }

        private void Draw2(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(Tracer, _end, null, _color, Angle, Origin, _scale, SpriteEffects.None, Layer);
            //spriteBatch.Draw(Game1.Pixel, _end, null, Color.Red, 0, Game1.PixelOrigin, new Vector2(3), SpriteEffects.None, Layer);
        }
    }
}