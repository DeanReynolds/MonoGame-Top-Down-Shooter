using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using Top_Down_Shooter.Scenes;

namespace Top_Down_Shooter
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        public const long ActiveFrameRate = (TimeSpan.TicksPerSecond / 60);
        public const long InactiveFrameRate = (TimeSpan.TicksPerSecond / 30);

#if DEBUG
        public static long WorldBakeDrawCount;
        public static long WorldBakeTextureCount;
        public static long WorldBakeSpriteCount;
        public static long WorldBakePrimitiveCount;
        public static long WorldBakeTargetCount;
#endif

        public static Scene Scene { get; internal set; }

        public static int VirtualWidth { get; private set; }
        public static int VirtualHeight { get; private set; }
        public static float VirtualScale { get; private set; }
        public static Viewport Viewport { get; private set; }
        public static Texture2D Pixel { get; private set; }
        public static Vector2 PixelOrigin { get; private set; }
        public static SpriteFont Font { get; private set; }
        public static Texture2D ShadowMap { get; private set; }
        public static Rectangle[] ShadowSource { get; private set; }
        public static bool DebugDraw { get; private set; }
        public static D2DMenu DebugMenu { get; private set; }
        public static float Angle90 { get; private set; }
        public static float Angle180 { get; private set; }
        public static float Angle270 { get; private set; }

        public static readonly Texture2D BulletTracer;

        public static int WindowWidth { get { return Program.Game.Window.ClientBounds.Width; } }
        public static int WindowHeight { get { return Program.Game.Window.ClientBounds.Height; } }
        public static int PreferredBackBufferWidth { get { return Program.Game.Services.GetService<GraphicsDeviceManager>().PreferredBackBufferWidth; } }
        public static int PreferredBackBufferHeight { get { return Program.Game.Services.GetService<GraphicsDeviceManager>().PreferredBackBufferHeight; } }

        internal static Random Random { get; private set; }

        private static GraphicsDeviceManager _graphics;
        private static SpriteBatch _spriteBatch;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = false
            };
            VirtualWidth = _graphics.PreferredBackBufferWidth;
            VirtualHeight = _graphics.PreferredBackBufferHeight;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
            Services.AddService(_graphics);
            Services.AddService(_spriteBatch);
            Services.AddService(Content);
            if (IsActive)
                OnActivated(this, EventArgs.Empty);
            else
                OnDeactivated(this, EventArgs.Empty);
            Random = new Random();
            IsMouseVisible = true;
            //IsFixedTimeStep = false;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.HardwareModeSwitch = false;
            _graphics.IsFullScreen = true;
            //_graphics.PreferredBackBufferWidth = 960;
            //_graphics.PreferredBackBufferHeight = 540;
            _graphics.ApplyChanges();
            SetVirtualResolution(960, 540);
            PixelOrigin = new Vector2(.5f);
            ShadowSource = new Rectangle[19];
            for (var i = 0; i < 18; i++)
                ShadowSource[i + 1] = new Rectangle(((i % 2) * Shadow.TextureSize), ((i / 2) * Shadow.TextureSize), Shadow.TextureSize, Shadow.TextureSize);
            DebugMenu = new D2DMenu("Debug Menu", Color.White, .666f);
            DebugMenu.AddGroup("D2D");
            DebugMenu.Groups["D2D"].AddItem("Profiler");
            DebugMenu.Groups["D2D"].Items["Profiler"].AddOption("On", Color.LimeGreen);
            DebugMenu.Groups["D2D"].Items["Profiler"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].AddItem("Physics");
            DebugMenu.Groups["D2D"].Items["Physics"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].Items["Physics"].AddOption("On", Color.LimeGreen);
            DebugMenu.Groups["D2D"].AddItem("Line to Grid");
            DebugMenu.Groups["D2D"].Items["Line to Grid"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].Items["Line to Grid"].AddOption("On", Color.LimeGreen);
            DebugMenu.Groups["D2D"].AddItem("Player Hitcrosses");
            DebugMenu.Groups["D2D"].Items["Player Hitcrosses"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].Items["Player Hitcrosses"].AddOption("On", Color.LimeGreen);
            DebugMenu.Groups["D2D"].AddItem("Player Info");
            DebugMenu.Groups["D2D"].Items["Player Info"].AddOption("On", Color.LimeGreen);
            DebugMenu.Groups["D2D"].Items["Player Info"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].AddItem("Player Spatial Hash");
            DebugMenu.Groups["D2D"].Items["Player Spatial Hash"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].Items["Player Spatial Hash"].AddOption("On", Color.LimeGreen);
            DebugMenu.Groups["D2D"].AddItem("Chunk Bound Lines");
            DebugMenu.Groups["D2D"].Items["Chunk Bound Lines"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].Items["Chunk Bound Lines"].AddOption("On", Color.LimeGreen);
            DebugMenu.Groups["D2D"].AddGroup("Shadows");
            DebugMenu.Groups["D2D"].Groups["Shadows"].AddItem("Angle");
            for (int i = 0; i < 360; i += 5)
                DebugMenu.Groups["D2D"].Groups["Shadows"].Items["Angle"].AddOption(i.ToString(), Color.White);
            DebugMenu.Groups["D2D"].Groups["Shadows"].AddGroup("Auto Rotate");
            DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].AddItem("Speed");
            DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].AddOption("Off", Color.Red);
            DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].AddOption("1 ms", Color.White);
            for (int i = 2; i <= 10; i += 2)
                DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].AddOption(string.Format("{0} ms", i), Color.White);
            for (int i = 15; i <= 50; i += 5)
                DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].AddOption(string.Format("{0} ms", i), Color.White);
            for (int i = 75; i <= 250; i += 25)
                DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].AddOption(string.Format("{0} ms", i), Color.White);
            for (int i = 300; i <= 500; i += 50)
                DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].AddOption(string.Format("{0} ms", i), Color.White);
            for (int i = 600; i <= 1000; i += 100)
                DebugMenu.Groups["D2D"].Groups["Shadows"].Groups["Auto Rotate"].Items["Speed"].AddOption(string.Format("{0} ms", i), Color.White);
            Angle90 = MathHelper.ToRadians(90);
            Angle180 = MathHelper.ToRadians(180);
            Angle270 = MathHelper.ToRadians(270);
            Scene = new Scenes.Menu();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            Pixel = new Texture2D(GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });
            Font = Content.Load<SpriteFont>("Fonts\\VCR OSD Mono");
            ShadowMap = Content.Load<Texture2D>("Textures\\shadowmap");
            Bullet.Tracer = Content.Load<Texture2D>("Textures\\bullet");
            Bullet.Origin = new Vector2(0, 4);
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            Keyboard.Update();
            Profiler.Start("Game Update");
            Scene?.Update(gameTime);
            Profiler.Stop("Game Update");
            if (Keyboard.Pressed(Keys.F3))
                DebugDraw = !DebugDraw;
            if (DebugDraw)
                DebugMenu.AcceptInput();
            if (DebugMenu.Groups["D2D"].Items["Profiler"].SelectedOption?.Text == "On")
                Profiler.Update(gameTime);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Profiler.Start("Game Draw");
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Viewport = Viewport;
            GraphicsDevice.Clear(Color.CornflowerBlue);
            Scene?.Draw(_spriteBatch, gameTime);
            Profiler.Stop("Game Draw");
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, null);
            string text = string.Format("FPS: {0}", Math.Floor(1 / gameTime.ElapsedGameTime.TotalSeconds));
            Vector2 textSize = Font.MeasureString(text);
            _spriteBatch.DrawString(Font, text, new Vector2((Viewport.Width - textSize.X - 3), 5), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            _spriteBatch.DrawString(Font, text, new Vector2((Viewport.Width - textSize.X - 4), 4), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
//#if DEBUG
//            text = string.Format("Game\n  Draw Count: {0}\n  Texture Count: {1}\n  Sprite Count: {2}\n  Primitive Count: {3}\n  Target Count: {4}\n\nWorld Bake\n  Draw Count: {5}\n  Texture Count: {6}\n  Sprite Count: {7}\n  Primitive Count: {8}\n  Target Count: {9}", GraphicsDevice.Metrics.DrawCount, GraphicsDevice.Metrics.TextureCount, GraphicsDevice.Metrics.SpriteCount, GraphicsDevice.Metrics.PrimitiveCount, GraphicsDevice.Metrics.TargetCount, WorldBakeDrawCount, WorldBakeTextureCount, WorldBakeSpriteCount, WorldBakePrimitiveCount, WorldBakeTargetCount);
//            _spriteBatch.DrawString(Font, text, new Vector2(5, 5), Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
//            _spriteBatch.DrawString(Font, text, new Vector2(4, 4), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
//#endif
            _spriteBatch.End();
            if (DebugMenu.Groups["D2D"].Items["Profiler"].SelectedOption?.Text == "On")
                Profiler.Draw(_spriteBatch, Font, Viewport.Width, Viewport.Height);
            if (DebugDraw)
            {
                _spriteBatch.Begin();
                DebugMenu.Draw(_spriteBatch, Font, new Vector2(8), 200);
                _spriteBatch.End();
            }
            base.Draw(gameTime);
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            TargetElapsedTime = new TimeSpan(ActiveFrameRate);
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            TargetElapsedTime = new TimeSpan(InactiveFrameRate);
            base.OnDeactivated(sender, args);
        }

        public static void SetVirtualResolution(int width, int height)
        {
            VirtualWidth = width;
            VirtualHeight = height;
            GraphicsDeviceManager graphicsDeviceManager = Program.Game.Services.GetService<GraphicsDeviceManager>();
            var targetAspectRatio = (width / (float)height);
            var width2 = graphicsDeviceManager.PreferredBackBufferWidth;
            var height2 = (int)(width2 / targetAspectRatio + .5f);
            if (height2 > graphicsDeviceManager.PreferredBackBufferHeight)
            {
                height2 = graphicsDeviceManager.PreferredBackBufferHeight;
                width2 = (int)(height2 * targetAspectRatio + .5f);
            }
            Viewport = new Viewport()
            {
                X = ((graphicsDeviceManager.PreferredBackBufferWidth / 2) - (width2 / 2)),
                Y = ((graphicsDeviceManager.PreferredBackBufferHeight / 2) - (height2 / 2)),
                Width = width2,
                Height = height2
            };
            VirtualScale = MathHelper.Min((graphicsDeviceManager.PreferredBackBufferWidth / (float)width), (graphicsDeviceManager.PreferredBackBufferHeight / (float)height));
        }
    }
}