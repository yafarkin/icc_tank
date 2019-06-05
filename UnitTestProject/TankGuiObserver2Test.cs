using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using TankGuiObserver2;

namespace UnitTestProject
{
    [TestClass]
    public class TankGuiObserver2Test
    {
        private Game _game;

        [TestInitialize]
        public void SetupContext()
        {
            _game = new Game("Game name", 1920, 1080);
            _game.RunGame();
            
        }

        [TestMethod]
        public void TestMethod1()
        {
        }
    }
}
