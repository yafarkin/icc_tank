#define Sandbox_0

using System;
using System.Xml;

namespace TankGuiObserver2
{
    class Program
    {
        /*
         ws://10.22.2.132:2000
             */
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
            string neadedValue = "";
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("TankGuiObserver2.exe.config");
            // получим корневой элемент
            XmlElement xRoot = xDoc.DocumentElement;
            // обход всех узлов в корневом элементе
            foreach (XmlNode xnode in xRoot)
            {
                foreach (XmlNode childnode in xnode.ChildNodes)
                {
                    if (childnode.Name == "add")
                    {
                        xDoc.WriteTo(null);
                        string data_to_parse = childnode.OuterXml;
                        if (childnode.OuterXml.Contains("ws"))
                        {
                            string[] splitted = data_to_parse.Split('"');
                            foreach (var s in splitted)
                            {
                                if (s.Contains("ws"))
                                {
                                    neadedValue = s;
                                }
                            }
                        }
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine("BLALA: ", neadedValue.Equals("ws://10.22.2.120:2000"));
            Console.Read();
#endif
        }
    }
}