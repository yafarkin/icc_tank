#define Sandbox_0

namespace TankGuiObserver2
{
    /*
        **********************
        ** HOW IT ALL WORKS **
        **********************

        EntryPointClass 
        -> Создаем Game instance в using блоке (чтобы по выходу из блока {} был вызван game.Dispose() 
           -> call: .ctor [Game]
              .ctor [Game]
              -> Инициализирует поля для render'а, соединения с сервером и блабла 
           -> call: RunGame()
              RunGame()
              -> call: RenderTarget.Begin()
              -> Смотрит было ли совершено нажатие на кнопку
                 -> Обрабатывает нажатие
              -> Смотрит требуется ли повторно подключиться к серверу
                 -> повторно подключается к серверу
              -> If Map обновлена
                 -> обновить Map в GameRender
              -> If EnterIsPressed и Map not null
                 -> call: DrawGame()
              -> Else
                 -> call: DrawWaitingSreen();
              -> call: RenderTarget.End()
    */

    /// <summary>
    /// Entry point class
    /// </summary>
    class Program
    {
        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">
        /// Аргументы командной строки, мы это не используем (пока что)
        /// </param>
        static void Main(string[] args)
        {
#if Sandbox_0
            System.Drawing.Size screenSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
            using (Game game = new Game("Battle city v0.1",
                screenSize.Width, 
                screenSize.Height, false))
            {
                game.RunGame();
            }
#else
            string filename = "TankGuiObserver2.exe.config";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(filename);
            XmlElement xRoot = xDoc.DocumentElement;
            
            foreach (XmlNode xnode in xRoot)
            {
                foreach (XmlNode childnode in xnode.ChildNodes)
                {
                    if (childnode.Name == "add")
                    {
                        foreach (XmlAttribute attribute in childnode.Attributes)
                        {
                            string attributeToString = attribute.ToString();
                            if (attribute.Name.Equals("value") && 
                                attribute.Value.Contains("ws://"))
                            {
                                attribute.Value = "ws://10.22.2.123:2000";
                            }
                        }
                    }
                }
            }
            xDoc.Save(filename);

            Console.Read();
#endif
        }
    }
}