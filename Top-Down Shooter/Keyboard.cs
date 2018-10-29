using Microsoft.Xna.Framework.Input;

namespace Top_Down_Shooter
{
    using Keys = Microsoft.Xna.Framework.Input.Keys;

    public static class Keyboard
    {
        private static KeyboardState _state;
        private static KeyboardState _lastState;

        public static void Update()
        {
            _lastState = _state;
            _state = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        }

        public static bool Pressed(Keys key) { return (_state.IsKeyDown(key) && ((_lastState == null) || _lastState.IsKeyUp(key))); }
        public static bool Released(Keys key) { return (_state.IsKeyUp(key) && ((_lastState != null) && _lastState.IsKeyDown(key))); }
        public static bool Holding(Keys key) { return _state.IsKeyDown(key); }
        public static bool PressedShift() { return (Pressed(Keys.LeftShift) || Pressed(Keys.RightShift)); }
        public static bool ReleasedShift() { return (Released(Keys.LeftShift) || Released(Keys.RightShift)); }
        public static bool HoldingShift() { return (Holding(Keys.LeftShift) || Holding(Keys.RightShift)); }
        public static bool PressedControl() { return (Pressed(Keys.LeftControl) || Pressed(Keys.RightControl)); }
        public static bool ReleasedControl() { return (Released(Keys.LeftControl) || Released(Keys.RightControl)); }
        public static bool HoldingControl() { return (Holding(Keys.LeftControl) || Holding(Keys.RightControl)); }
    }
}