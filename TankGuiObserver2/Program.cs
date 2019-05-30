namespace TankGuiObserver2
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Drawing.Size screenSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
            using (Game game = new Game("Battle city v0.1",
                screenSize.Width, screenSize.Height, false))
            {
                game.RunGame();
            }
        }
    }
}