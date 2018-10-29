using Microsoft.Xna.Framework;
using System;

namespace Top_Down_Shooter
{
#if WINDOWS || LINUX
    public static class Program
    {
        internal static Game Game { get; private set; }

        [STAThread]
        static void Main()
        {
            using (var game = new Game1())
            {
                Game = game;
                game.Run();
            }
        }
    }
#endif
}