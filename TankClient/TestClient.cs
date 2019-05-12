using System;
using TankCommon;
using TankCommon.Enum;
using TankCommon.Objects;

namespace TankClient
{
    public class TestClient : IClientBot
    {
        protected readonly Random _random = new Random();
        protected Rectangle rectangle;
        protected Map _map;
        private int upgradeX;
        private int upgradeY;

        public ServerResponse Client(int msgCount, ServerRequest request)
        {
            //Если карта существует присвоить локальной карте карту
            if (request.Map.Cells != null)
            {
                _map = request.Map;
            }
            //Если в карте ничего нет, то просить обновления карты
            else if (null == _map)
            {
                return new ServerResponse { ClientCommand = ClientCommandType.UpdateMap };
            }

            //записать в карту все интерактивные объекты
            _map.InteractObjects = request.Map.InteractObjects;

            var myTank = request.Tank;
            if (null == myTank)
            {
                return new ServerResponse { ClientCommand = ClientCommandType.None };
            }

            //если размер танка не известен, то узнать
            if (null == rectangle)
            {
                rectangle = myTank.Rectangle;
            }

            rectangle = myTank.Rectangle;

            //если есть интерактивные объекты
            if (_map.InteractObjects != null)
            {
                foreach (var elem in _map.InteractObjects)
                {
                    //если элемент это улучшение, то присвоить  upgradeX его координату по Х
                    if (elem is UpgradeInteractObject upgradeInteractObject)
                    {
                        upgradeX = upgradeInteractObject.Rectangle.LeftCorner.LeftInt;
                        upgradeY = upgradeInteractObject.Rectangle.LeftCorner.TopInt;
                    }
                }
            }

            return FindUpdateAndGoTo(_map, myTank, upgradeX, upgradeY);
            
            //return new ServerResponse { ClientCommand = ClientCommandType.None };
        }

        private static ServerResponse FindUpdateAndGoTo(Map map, TankObject myTank, int upgradeX, int upgradeY)
        {
            if (map.InteractObjects != null)
            {
                //Если левый угол моего танка по Х меньше, чем левый угол улучшающего объекта и я не повёрнут направо
                if (myTank.Rectangle.LeftCorner.LeftInt < upgradeX && myTank.Direction != DirectionType.Right)
                {
                    //повернуть направо
                    return new ServerResponse { ClientCommand = ClientCommandType.TurnRight };
                }
                else
                {
                    //Если мой левый угол меньше, чем левый угол  апгрейда
                    if (myTank.Rectangle.LeftCorner.Left < upgradeX)
                    {
                        //Ехать
                        return new ServerResponse { ClientCommand = ClientCommandType.Go };
                    }
                }
            
                //Если левый угол моего танка по Х меньше, чем левый угол улучшающего объекта и я не повёрнут на
                if (myTank.Rectangle.LeftCorner.LeftInt > upgradeX && myTank.Direction != DirectionType.Left)     
                {
                    //повернуть направо
                    return new ServerResponse { ClientCommand = ClientCommandType.TurnLeft };
                }
                else
                {
                    //Если мой левый угол меньше, чем левый угол  апгрейда
                    if (myTank.Rectangle.LeftCorner.Left > upgradeX)
                    {
                        //Ехать
                        return new ServerResponse { ClientCommand = ClientCommandType.Go };
                    }
                }

                //Если левый угол моего танка по Y меньше, чем левый угол улучшающего объекта и я не повёрнут вниз
                if (myTank.Rectangle.LeftCorner.TopInt > upgradeY && myTank.Direction != DirectionType.Down)     
                {
                    //повернуть вниз
                    return new ServerResponse { ClientCommand = ClientCommandType.TurnDown };
                }
                else
                {
                    //Если мой левый угол меньше по Y, чем левый угол  апгрейда
                    if (myTank.Rectangle.LeftCorner.TopInt < upgradeY)
                    {
                        //Ехать
                        return new ServerResponse { ClientCommand = ClientCommandType.Go };
                    }
                }

                //Если левый угол моего танка по Х меньше, чем левый угол улучшающего объекта и я не повёрнут на
                if (myTank.Rectangle.LeftCorner.TopInt > upgradeY && myTank.Direction != DirectionType.Up)     
                {
                    //повернуть направо
                    return new ServerResponse { ClientCommand = ClientCommandType.TurnUp };
                }
                else
                {
                    //Если мой левый угол меньше, чем левый угол  апгрейда
                    if (myTank.Rectangle.LeftCorner.TopInt > upgradeY)
                    {
                        //Ехать
                        return new ServerResponse { ClientCommand = ClientCommandType.Go };
                    }
                    else
                    {
                        return new ServerResponse { ClientCommand = ClientCommandType.Stop };
                    }
                }
            }

            return new ServerResponse { ClientCommand = ClientCommandType.None };
        }
    }
}