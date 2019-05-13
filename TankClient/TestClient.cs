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
        private delegate ServerResponse Script();
        private static int stepsBeforeOver = 0;
        private static  Script delegateScript = Stop;

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
                        //upgradeX = upgradeInteractObject.Rectangle.LeftCorner.LeftInt;
                        //upgradeY = upgradeInteractObject.Rectangle.LeftCorner.TopInt;
                    }
                }
            }

            return GoToPoint(_map, myTank);
            
            //return new ServerResponse { ClientCommand = ClientCommandType.None };
        }

        private static ServerResponse GoToPoint(Map map, TankObject myTank)
        {
            if (map.InteractObjects != null)
            {
                int destinationX;
                int destinationY;
                var nearestObj = FindNearestObj(map, myTank);
                destinationX = nearestObj.Rectangle.WidthInt;
                destinationY = nearestObj.Rectangle.HeightInt;

                if ( Math.Abs(destinationX - GetMyX(myTank)) > Math.Abs(destinationY - GetMyY(myTank)) )
                {
                    //Если левый угол моего танка по Х меньше, чем левый угол достигаемого объекта и я не повёрнут направо
                    if (GetMyX(myTank) < destinationX && myTank.Direction != DirectionType.Right)
                    {
                        //повернуть направо
                        return TurnRight();
                    }
                    else
                    {
                        //Если мой левый угол меньше, чем левый угол  апгрейда
                        if (GetMyX(myTank) < destinationX)
                        {
                            //Ехать
                            return Go();
                        }
                    }

                    //Если левый угол моего танка по Х больше, чем левый угол достигаемого объекта и я не повёрнут на
                    if (GetMyX(myTank) > destinationX && myTank.Direction != DirectionType.Left)
                    {
                        //повернуть направо
                        return TurnLeft();
                    }
                    else
                    {
                        //Если мой левый угол меньше, чем левый угол  апгрейда
                        if (GetMyX(myTank) > destinationX)
                        {
                            //Ехать
                            return Go();
                        }
                    }
                }
                else
                {
                    //Если левый угол моего танка по Y меньше, чем левый угол улучшающего объекта и я не повёрнут вниз
                    if (GetMyY(myTank) < destinationY && myTank.Direction != DirectionType.Down)
                    {
                        //повернуть вниз
                        return TurnDown();
                    }
                    else
                    {
                        //Если мой левый угол меньше по Y, чем левый угол  апгрейда
                        if (GetMyY(myTank) < destinationY)
                        {
                            //Ехать
                            return Go();
                        }
                    }
                    
                    //Если левый угол моего танка по Y больше, чем левый угол улучшающего объекта и я не повёрнут вверх
                    if (GetMyY(myTank) > destinationY && myTank.Direction != DirectionType.Up)
                    {
                        //повернуть вверх
                        return TurnUp();
                    }
                    else
                    {
                        //Если мой левый угол меньше, чем левый угол  апгрейда
                        if (GetMyY(myTank) > destinationY)
                        {
                            //Ехать
                            return Go();
                        }
                        else
                        {
                            return Stop();
                        }
                    }
                }
            }

            return new ServerResponse { ClientCommand = ClientCommandType.None };
        }

        private static BaseInteractObject FindNearestObj(Map map, TankObject myTank)
        {
            int shortestDistToElem = map.MapHeight;
            BaseInteractObject nearestObject = map.InteractObjects[0];

            //Если есть интерактивные объекты
            if (map.InteractObjects != null)
            {
                foreach (var elem in map.InteractObjects)
                {
                    //Если этот элемент это upgrade
                    if (elem is UpgradeInteractObject)
                    {
                        var distToElem = (Math.Abs(elem.Rectangle.LeftCorner.LeftInt - GetMyX(myTank)) + Math.Abs(elem.Rectangle.LeftCorner.TopInt - GetMyY(myTank)));
                        if (distToElem < shortestDistToElem)
                        {
                            shortestDistToElem = distToElem;
                            nearestObject = elem;
                        }
                    }
                }
            }
            return nearestObject;
        }

        /// <summary>
        /// Переводит карту в массив лишь с проходимыми и не проходимыми полями
        /// </summary>
        /// <param name="map"></param>
        /// <returns></returns>
        private static int [,] TranslMapForPassfind(Map map)
        {
            var transArr = new int[map.MapHeight, map.MapWidth];
            var arrXLen = transArr.GetLength(0);
            var arrYLen = transArr.GetLength(1);
            for (var x = 0; x < arrXLen; x++)
            {
                for (var y = 0; y < arrYLen; y++) {
                    if (map.Cells[x,y].Equals(CellMapType.Grass) || map.Cells[x, y].Equals(CellMapType.Void))
                    {
                        transArr[x, y] = 0;
                    }
                    else
                    {
                        transArr[x, y] = -1;
                    }
                }
            }

            return transArr;
        }

        /// <summary>
        /// Принимает направление и передвигает туда танк
        /// </summary>
        /// <param name="direction"> 0 = вниз| 1 = вправо| 2 = вверх| 3 = влево</param>
        private static ServerResponse EasyMove(int direction)
        {
            if(direction == 0 && stepsBeforeOver == 0)
            {
                delegateScript = TurnDown;
                stepsBeforeOver = 2;
            }
            if (direction == 1 && stepsBeforeOver == 0)
            {
                delegateScript = TurnRight;
                stepsBeforeOver = 2;
            }
            if (direction == 2 && stepsBeforeOver == 0)
            {
                delegateScript = TurnUp;
                stepsBeforeOver = 2;
            }
            if (direction == 3 && stepsBeforeOver == 0)
            {
                delegateScript = TurnLeft;
                stepsBeforeOver = 2;
            }
            if (stepsBeforeOver > 0)
            {
                stepsBeforeOver--;
            }

            return delegateScript();
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