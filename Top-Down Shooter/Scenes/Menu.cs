using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using VelcroPhysics.Dynamics;
using VelcroPhysics.Factories;
using VelcroPhysics.Utilities;

namespace Top_Down_Shooter.Scenes
{
    public class Menu : Scene
    {
        public override void Update(GameTime gameTime)
        {
            if (Program.Game.IsActive)
            {
                if (Net.Peer == null)
                    if (Keyboard.Pressed(Keys.F1))
                    {
                        Game1.Scene = new Scenes.Game(32);
                        Net.Host(6121, Scenes.Game.PlayerCount);
                        PlayerSpatialHash.Add(Scenes.Game.Self = Scenes.Game.AddPlayer(null));
                        Net.PlayerState[Scenes.Game.Self.ID] = Player.NetState.Playing;
                        Scenes.Game.MakeServer();
                    }
                    else if (Keyboard.Pressed(Keys.F2))
                    {
                        Net.Connect("25.73.181.13", 6121);
                        Scenes.Game.MakeClient();
                    }
            }
            Net.Update();
            base.Update(gameTime);
        }
    }
}