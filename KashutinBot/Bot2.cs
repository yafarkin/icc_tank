using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;

namespace TankClient
{
    class Bot2 : IClientBot
    {
        protected Rectangle rectangle;
        protected Map _map;
        //private delegate ServerResponse Script();
        //private static int stepsBeforeOver = 0;
        //private static Script delegateScript = Stop;
        //private static Rectangle lastRectangle;

        public ServerResponse Client(int msgCount, ServerRequest request)
        {
            //Если карта существует присвоить карте карту из ответа сервера
            if (request.Map.Cells != null)
            {
                _map = request.Map;
                //var translMap = TranslMapForPassfind(_map);
            }
            //Если в карте ничего нет, то просить обновления карты
            else if (_map == null)
            {
                return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
            }

            //Записать в карту все интерактивные объекты из ответа сервера
            _map.InteractObjects = request.Map.InteractObjects;

            var myTank = request.Tank;
            if (null == myTank)
            {
                return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
            }

            //если размер танка не известен, то узнать
            if (null == rectangle)
            {
                rectangle = myTank.Rectangle;
            }

            rectangle = myTank.Rectangle;

            return None(); /*BeAliveFirtStep(_map, myTank);*/

        }

        private static ServerResponse Fire()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.Fire };
        }

        private static ServerResponse TurnRight()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnRight };
        }

        private static ServerResponse TurnLeft()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnLeft };
        }

        private static ServerResponse TurnUp()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnUp };
        }

        private static ServerResponse TurnDown()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.TurnDown };
        }

        private static ServerResponse Go()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.Go };
        }

        private static ServerResponse Stop()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.Stop };
        }

        private static ServerResponse None()
        {
            return new ServerResponse { ClientCommand = ClientCommandType.None };
        }

        private static int GetMyX(TankObject tank)
        {
            return tank.Rectangle.LeftCorner.LeftInt;
        }

        private static int GetMyY(TankObject tank)
        {
            return tank.Rectangle.LeftCorner.TopInt;
        }
    }
}
