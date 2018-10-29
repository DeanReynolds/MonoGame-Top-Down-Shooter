using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using VelcroPhysics.Utilities;

namespace Top_Down_Shooter
{
    public class Camera
    {
        public float X
        {
            get { return _position.X; }
            set
            {
                _simPosition.X = ConvertUnits.ToSimUnits(_position.X = value);
                UpdateRotX();
                UpdatePositionInTransform();
            }
        }
        public float Y
        {
            get { return _position.Y; }
            set
            {
                _simPosition.Y = ConvertUnits.ToSimUnits(_position.Y = value);
                UpdateRotY();
                UpdatePositionInTransform();
            }
        }
        public Vector2 Position
        {
            get { return _position; }
            set
            {
                _simPosition.X = ConvertUnits.ToSimUnits(_position.X = value.X);
                _simPosition.Y = ConvertUnits.ToSimUnits(_position.Y = value.Y);
                UpdateRotX();
                UpdateRotY();
                UpdatePositionInTransform();
            }
        }
        public float Angle
        {
            get { return _angle; }
            set
            {
                _rotM11 = (float)System.Math.Cos(-(_angle = value));
                _rotM12 = (float)System.Math.Sin(-_angle);
                UpdateScaleInTransform();
                UpdateRotX();
                UpdateRotY();
                UpdatePositionInTransform();
            }
        }
        public Vector2 Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                UpdateScaleInTransform();
                UpdateRotX();
                UpdateRotY();
                UpdatePositionInTransform();
            }
        }
        public Vector2 ScreenSize
        {
            get { return _screenSize; }
            set
            {
                _screenSize = value;
                _simScreenCenter.X = ConvertUnits.ToSimUnits(_screenCenter.X = (_screenSize.X / 2f));
                _simScreenCenter.Y = ConvertUnits.ToSimUnits(_screenCenter.Y = (_screenSize.Y / 2f));
                UpdatePositionInTransform();
                _projection.M11 = (float)(2d / _screenSize.X);
                _projection.M22 = (float)(2d / -_screenSize.Y);
                Vector2 _simScreenSize = new Vector2(ConvertUnits.ToSimUnits(_screenSize.X), ConvertUnits.ToSimUnits(_screenSize.Y));
                _simProjection.M11 = (float)(2d / _simScreenSize.X);
                _simProjection.M22 = (float)(2d / -_simScreenSize.Y);
            }
        }

        public Matrix Transform { get { return _transform; } }
        public Matrix SimTransform { get { return _simTransform; } }
        public Vector2 MousePosition { get { return _mousePosition; } }
        public Matrix Projection { get { return _projection; } }
        public Matrix SimProjection { get { return _simProjection; } }

        private Vector2 _position;
        private Vector2 _simPosition;
        private float _angle;
        private Vector2 _screenSize;
        private Vector2 _screenCenter;
        private Vector2 _simScreenCenter;
        private float _rotM11;
        private float _rotM12;
        private float _rotX1;
        private float _rotY1;
        private float _rotX2;
        private float _rotY2;
        private float _simRotX1;
        private float _simRotY1;
        private float _simRotX2;
        private float _simRotY2;
        private Vector2 _scale;
        private double _n27;
        private Matrix _transform;
        private Matrix _simTransform;
        private float _invertM11;
        private float _invertM12;
        private float _invertM21;
        private float _invertM22;
        private float _invertM41;
        private float _invertM42;
        private Vector2 _mousePosition;
        private Matrix _projection;
        private Matrix _simProjection;

        public Camera(float angle = 0, float scale = 1) : this(Vector2.Zero, angle, new Vector2(scale)) { }
        public Camera(float angle, Vector2 scale) : this(Vector2.Zero, angle, scale) { }
        public Camera(Vector2 position, float angle = 0, float scale = 1) : this(position, angle, new Vector2(scale)) { }
        public Camera(Vector2 position, float angle) : this(position, angle, Vector2.One) { }
        public Camera(Vector2 position, float angle, Vector2 scale)
        {
            _simPosition.X = ConvertUnits.ToSimUnits(_position.X = position.X);
            _simPosition.Y = ConvertUnits.ToSimUnits(_position.Y = position.Y);
            _rotM11 = (float)System.Math.Cos(-(_angle = angle));
            _rotM12 = (float)System.Math.Sin(-_angle);
            _scale = scale;
            _screenSize = new Vector2(Game1.Viewport.Width, Game1.Viewport.Height);
            _simScreenCenter.X = ConvertUnits.ToSimUnits(_screenCenter.X = (_screenSize.X / 2f));
            _simScreenCenter.Y = ConvertUnits.ToSimUnits(_screenCenter.Y = (_screenSize.Y / 2f));
            _transform = new Matrix
            {
                M33 = 1,
                M44 = 1
            };
            _simTransform = new Matrix
            {
                M33 = 1,
                M44 = 1
            };
            UpdateScaleInTransform();
            UpdateRotX();
            UpdateRotY();
            UpdatePositionInTransform();
            _projection = new Matrix
            {
                M11 = (float)(2d / _screenSize.X),
                M22 = (float)(2d / -_screenSize.Y),
                M33 = -1,
                M41 = -1,
                M42 = 1,
                M44 = 1
            };
            _simProjection = new Matrix
            {
                M11 = (float)(2d / ConvertUnits.ToSimUnits(_screenSize.X)),
                M22 = (float)(2d / -ConvertUnits.ToSimUnits(_screenSize.Y)),
                M33 = -1,
                M41 = -1,
                M42 = 1,
                M44 = 1
            };
        }

        public void UpdateMousePosition(MouseState? mouseState = null)
        {
            if (!mouseState.HasValue)
                mouseState = Mouse.GetState();
            float mouseX = mouseState.Value.Position.X;
            float mouseY = mouseState.Value.Position.Y;
            _mousePosition.X = ((mouseX * _invertM11) + (mouseY * _invertM21) + _invertM41);
            _mousePosition.Y = ((mouseX * _invertM12) + (mouseY * _invertM22) + _invertM42);
        }

        private void UpdateRotX()
        {
            float m41 = (-_position.X * _scale.X);
            _rotX1 = (m41 * _rotM11);
            _rotX2 = (m41 * _rotM12);
            m41 = (-_simPosition.X * _scale.X);
            _simRotX1 = (m41 * _rotM11);
            _simRotX2 = (m41 * _rotM12);
        }

        private void UpdateRotY()
        {
            float m42 = (-_position.Y * _scale.Y);
            _rotY1 = (m42 * -_rotM12);
            _rotY2 = (m42 * _rotM11);
            m42 = (-_simPosition.Y * _scale.Y);
            _simRotY1 = (m42 * -_rotM12);
            _simRotY2 = (m42 * _rotM11);
        }

        private void UpdatePositionInTransform()
        {
            _transform.M41 = ((_rotX1 + _rotY1) + _screenCenter.X);
            _transform.M42 = ((_rotX2 + _rotY2) + _screenCenter.Y);
            _invertM41 = (float)(-((double)_transform.M21 * -_transform.M42 - (double)_transform.M22 * -_transform.M41) * _n27);
            _invertM42 = (float)(((double)_transform.M11 * -_transform.M42 - (double)_transform.M12 * -_transform.M41) * _n27);
            _simTransform.M41 = ((_simRotX1 + _simRotY1) + _simScreenCenter.X);
            _simTransform.M42 = ((_simRotX2 + _simRotY2) + _simScreenCenter.Y);
        }

        private void UpdateScaleInTransform()
        {
            _simTransform.M11 = _transform.M11 = (_scale.X * _rotM11);
            _simTransform.M12 = _transform.M12 = (_scale.Y * _rotM12);
            _simTransform.M21 = _transform.M21 = (_scale.X * -_rotM12);
            _simTransform.M22 = _transform.M22 = (_scale.Y * _rotM11);
            _n27 = (1d / ((double)_transform.M11 * _transform.M22 + (double)_transform.M12 * -_transform.M21));
            _invertM11 = (float)(_transform.M22 * _n27);
            _invertM21 = (float)(-_transform.M21 * _n27);
            _invertM12 = (float)-(_transform.M12 * _n27);
            _invertM22 = (float)(_transform.M11 * _n27);
        }
    }
}