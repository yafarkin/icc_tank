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